using Microsoft.EntityFrameworkCore;

namespace MiniApp.Data;

public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<MiniAppUser?> FindByTelegramUserIdAsync(long telegramUserId, CancellationToken ct)
        => _db.Users.SingleOrDefaultAsync(x => x.TelegramUserId == telegramUserId, ct);

    public async Task<MiniAppUser> UpsertByTelegramUserIdAsync(long telegramUserId, CancellationToken ct, string? acquisitionDeepLink = null)
    {
        var normalizedDeepLink = NormalizeDeepLink(acquisitionDeepLink);
        var existing = await _db.Users.SingleOrDefaultAsync(x => x.TelegramUserId == telegramUserId, ct);
        if (existing is not null)
        {
            // The mini app fires several /api calls at once on launch, each upserting this user
            // to bump LastSeenAtUtc. The user row is under optimistic concurrency (xmin), so
            // simultaneous bumps race: the loser's UPDATE affects 0 rows and throws. The bump is
            // best-effort, so reload the current row and retry rather than 500-ing the request.
            for (var attempt = 0; ; attempt++)
            {
                if (string.IsNullOrWhiteSpace(existing.AcquisitionDeepLink) && !string.IsNullOrWhiteSpace(normalizedDeepLink))
                    existing.AcquisitionDeepLink = normalizedDeepLink;

                existing.LastSeenAtUtc = DateTimeOffset.UtcNow;

                try
                {
                    await _db.SaveChangesAsync(ct);
                    return existing;
                }
                catch (DbUpdateConcurrencyException) when (attempt < 5)
                {
                    // Refresh the tracked entity (including its xmin) from the row a concurrent
                    // request just wrote, then re-apply our changes and try again.
                    await _db.Entry(existing).ReloadAsync(ct);
                }
            }
        }

        var user = new MiniAppUser
        {
            TelegramUserId = telegramUserId,
            AcquisitionDeepLink = normalizedDeepLink,
            ReferredByUserIdOrUnbound = MiniAppUser.UnboundReferralUserId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            LastSeenAtUtc = DateTimeOffset.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return user;
    }

    public async Task<MiniAppUser> SetNumberAsync(long telegramUserId, string number, CancellationToken ct)
    {
        var user = await UpsertByTelegramUserIdAsync(telegramUserId, ct);
        return await SaveUserWithRetryAsync(user, () => user.Number = number, ct);
    }

    public async Task<MiniAppUser> SetPreferredLanguageAsync(long telegramUserId, string preferredLanguage, CancellationToken ct)
    {
        var user = await UpsertByTelegramUserIdAsync(telegramUserId, ct);
        return await SaveUserWithRetryAsync(user, () => user.PreferredLanguage = preferredLanguage, ct);
    }

    /// <summary>
    /// Applies a mutation to a tracked user plus the LastSeenAtUtc bump and saves, retrying on
    /// optimistic-concurrency (xmin) conflicts. Concurrent launch requests touch the same user
    /// row, so a lone field update must not 500 just because another request wrote first; on a
    /// conflict the row is reloaded and the mutation re-applied against the fresh xmin.
    /// </summary>
    private async Task<MiniAppUser> SaveUserWithRetryAsync(MiniAppUser user, Action apply, CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
        {
            apply();
            user.LastSeenAtUtc = DateTimeOffset.UtcNow;

            try
            {
                await _db.SaveChangesAsync(ct);
                return user;
            }
            catch (DbUpdateConcurrencyException) when (attempt < 5)
            {
                await _db.Entry(user).ReloadAsync(ct);
            }
        }
    }

    private static string? NormalizeDeepLink(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (trimmed.Length > 128)
            trimmed = trimmed[..128];

        return trimmed;
    }
}

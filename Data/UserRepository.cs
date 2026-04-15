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
            if (string.IsNullOrWhiteSpace(existing.AcquisitionDeepLink) && !string.IsNullOrWhiteSpace(normalizedDeepLink))
                existing.AcquisitionDeepLink = normalizedDeepLink;

            existing.LastSeenAtUtc = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(ct);
            return existing;
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
        user.Number = number;
        user.LastSeenAtUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return user;
    }

    public async Task<MiniAppUser> SetPreferredLanguageAsync(long telegramUserId, string preferredLanguage, CancellationToken ct)
    {
        var user = await UpsertByTelegramUserIdAsync(telegramUserId, ct);
        user.PreferredLanguage = preferredLanguage;
        user.LastSeenAtUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return user;
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

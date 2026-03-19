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

    public async Task<MiniAppUser> UpsertByTelegramUserIdAsync(long telegramUserId, CancellationToken ct)
    {
        var existing = await _db.Users.SingleOrDefaultAsync(x => x.TelegramUserId == telegramUserId, ct);
        if (existing is not null)
        {
            existing.LastSeenAtUtc = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(ct);
            return existing;
        }

        var user = new MiniAppUser
        {
            TelegramUserId = telegramUserId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            LastSeenAtUtc = DateTimeOffset.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return user;
    }
}


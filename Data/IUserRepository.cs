namespace MiniApp.Data;

public interface IUserRepository
{
    Task<MiniAppUser?> FindByTelegramUserIdAsync(long telegramUserId, CancellationToken ct);
    Task<MiniAppUser> UpsertByTelegramUserIdAsync(long telegramUserId, CancellationToken ct);
}


namespace MiniApp.Data;

public interface IUserService
{
    /// <summary>
    /// Ensures a user exists and updates LastSeenAtUtc.
    /// </summary>
    Task<MiniAppUser> TouchUserAsync(long telegramUserId, CancellationToken ct);
}


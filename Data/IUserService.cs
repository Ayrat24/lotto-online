namespace MiniApp.Data;

public interface IUserService
{
    /// <summary>
    /// Ensures a user exists and updates LastSeenAtUtc.
    /// </summary>
    Task<MiniAppUser> TouchUserAsync(long telegramUserId, CancellationToken ct);

    /// <summary>
    /// Ensures a user exists and stores the provided number.
    /// </summary>
    Task<MiniAppUser> SetNumberAsync(long telegramUserId, string number, CancellationToken ct);
}

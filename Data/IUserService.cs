namespace MiniApp.Data;

public interface IUserService
{
    /// <summary>
    /// Ensures a user exists and updates LastSeenAtUtc.
    /// </summary>
    Task<MiniAppUser> TouchUserAsync(long telegramUserId, CancellationToken ct, string? acquisitionDeepLink = null);

    /// <summary>
    /// Ensures a user exists and stores the provided number.
    /// </summary>
    Task<MiniAppUser> SetNumberAsync(long telegramUserId, string number, CancellationToken ct);

    /// <summary>
    /// Ensures a user exists and stores the preferred language.
    /// </summary>
    Task<MiniAppUser> SetPreferredLanguageAsync(long telegramUserId, string preferredLanguage, CancellationToken ct);
}

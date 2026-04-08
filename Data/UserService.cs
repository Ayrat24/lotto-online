namespace MiniApp.Data;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _repo;

    public UserService(IUserRepository repo)
    {
        _repo = repo;
    }

    public Task<MiniAppUser> TouchUserAsync(long telegramUserId, CancellationToken ct)
        => _repo.UpsertByTelegramUserIdAsync(telegramUserId, ct);

    public Task<MiniAppUser> SetNumberAsync(long telegramUserId, string number, CancellationToken ct)
        => _repo.SetNumberAsync(telegramUserId, number, ct);

    public Task<MiniAppUser> SetPreferredLanguageAsync(long telegramUserId, string preferredLanguage, CancellationToken ct)
        => _repo.SetPreferredLanguageAsync(telegramUserId, preferredLanguage, ct);
}

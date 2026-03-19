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
}

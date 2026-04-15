using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MiniApp.Admin;
using MiniApp.Data;
using MiniApp.Features.Localization;

namespace MiniApp.Pages.Admin;

[Authorize(Policy = AdminAuth.PolicyName)]
public sealed class DbModel : LocalizedAdminPageModel
{
    private readonly AppDbContext _db;

    public DbModel(AppDbContext db, ILocalizationService localization) : base(localization)
    {
        _db = db;
    }

    public string Provider { get; private set; } = "";
    public bool CanConnect { get; private set; }
    public long UsersCount { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        await LoadUiTextAsync(ct);
        Provider = _db.Database.ProviderName ?? "(unknown)";
        CanConnect = await _db.Database.CanConnectAsync(ct);
        UsersCount = await _db.Users.LongCountAsync(ct);
    }
}


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MiniApp.Admin;
using MiniApp.Data;

namespace MiniApp.Pages.Admin;

[Authorize(Policy = AdminAuth.PolicyName)]
public sealed class DbModel : PageModel
{
    private readonly AppDbContext _db;

    public DbModel(AppDbContext db)
    {
        _db = db;
    }

    public string Provider { get; private set; } = "";
    public bool CanConnect { get; private set; }
    public long UsersCount { get; private set; }

    public async Task OnGetAsync()
    {
        Provider = _db.Database.ProviderName ?? "(unknown)";
        CanConnect = await _db.Database.CanConnectAsync();
        UsersCount = await _db.Users.LongCountAsync();
    }
}


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MiniApp.Admin;
using MiniApp.Data;
using MiniApp.Features.Auth;

namespace MiniApp.Pages.Admin.Users;

[Authorize(Policy = AdminAuth.PolicyName)]
public sealed class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public IndexModel(AppDbContext db, IConfiguration config, IWebHostEnvironment env)
    {
        _db = db;
        _config = config;
        _env = env;
    }

    public List<MiniAppUser> Users { get; private set; } = new();

    public async Task OnGetAsync()
    {
        if (LocalDebugMode.TryGetDebugTelegramUserId(HttpContext, _config, _env, out var debugTelegramUserId))
            await LocalDebugSeed.EnsureSeededAsync(_db, debugTelegramUserId, HttpContext.RequestAborted);

        Users = await _db.Users
            .OrderByDescending(x => x.LastSeenAtUtc)
            .Take(200)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(long id, CancellationToken ct)
    {
        var u = await _db.Users.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (u is null)
            return RedirectToPage();

        _db.Users.Remove(u);
        await _db.SaveChangesAsync(ct);

        return RedirectToPage();
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MiniApp.Admin;
using MiniApp.Data;

namespace MiniApp.Pages.Admin.Users;

[Authorize(Policy = AdminAuth.PolicyName)]
public sealed class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public List<MiniAppUser> Users { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Users = await _db.Users
            .OrderByDescending(x => x.LastSeenAtUtc)
            .Take(200)
            .AsNoTracking()
            .ToListAsync();
    }
}


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MiniApp.Admin;
using MiniApp.Data;

namespace MiniApp.Pages.Admin.Users;

[Authorize(Policy = AdminAuth.PolicyName)]
public sealed class EditModel : PageModel
{
    private readonly AppDbContext _db;

    public EditModel(AppDbContext db)
    {
        _db = db;
    }

    public MiniAppUser? SelectedUser { get; private set; }

    [BindProperty]
    public long TelegramUserId { get; set; }

    [BindProperty]
    public string? Number { get; set; }

    [BindProperty]
    public DateTimeOffset LastSeenAtUtc { get; set; }

    public async Task OnGetAsync(long id)
    {
        SelectedUser = await _db.Users.SingleOrDefaultAsync(x => x.Id == id);
        if (SelectedUser is null) return;

        TelegramUserId = SelectedUser.TelegramUserId;
        Number = SelectedUser.Number;
        LastSeenAtUtc = SelectedUser.LastSeenAtUtc;
    }

    public async Task<IActionResult> OnPostAsync(long id)
    {
        var u = await _db.Users.SingleOrDefaultAsync(x => x.Id == id);
        if (u is null) return NotFound();

        u.TelegramUserId = TelegramUserId;
        u.Number = string.IsNullOrWhiteSpace(Number) ? null : Number.Trim();
        u.LastSeenAtUtc = LastSeenAtUtc;

        await _db.SaveChangesAsync();
        return RedirectToPage("/Admin/Users/Index");
    }
}

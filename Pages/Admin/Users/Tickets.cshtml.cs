using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MiniApp.Admin;
using MiniApp.Data;

namespace MiniApp.Pages.Admin.Users;

[Authorize(Policy = AdminAuth.PolicyName)]
public sealed class TicketsModel : PageModel
{
    private readonly AppDbContext _db;

    public TicketsModel(AppDbContext db)
    {
        _db = db;
    }

    public MiniAppUser? SelectedUser { get; private set; }
    public List<Ticket> Tickets { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken ct)
    {
        SelectedUser = await _db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (SelectedUser is null)
            return Page();

        Tickets = await _db.Tickets
            .Where(x => x.UserId == id)
            .OrderByDescending(x => x.PurchasedAtUtc)
            .AsNoTracking()
            .ToListAsync(ct);

        return Page();
    }

    public async Task<IActionResult> OnPostDeleteTicketAsync(long id, long ticketId, CancellationToken ct)
    {
        var t = await _db.Tickets.SingleOrDefaultAsync(x => x.Id == ticketId && x.UserId == id, ct);
        if (t is not null)
        {
            _db.Tickets.Remove(t);
            await _db.SaveChangesAsync(ct);
        }

        return RedirectToPage(new { id });
    }
}

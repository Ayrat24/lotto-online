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
    public string? AcquisitionDeepLink { get; set; }

    [BindProperty]
    public decimal Balance { get; set; }

    [BindProperty]
    public DateTimeOffset LastSeenAtUtc { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchPhone { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTelegramId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchDeepLink { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SortBy { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SortDir { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? PageNumber { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? PageSize { get; set; }

    public async Task OnGetAsync(long id)
    {
        SelectedUser = await _db.Users.SingleOrDefaultAsync(x => x.Id == id);
        if (SelectedUser is null) return;

        TelegramUserId = SelectedUser.TelegramUserId;
        Number = SelectedUser.Number;
        AcquisitionDeepLink = SelectedUser.AcquisitionDeepLink;
        Balance = SelectedUser.Balance;
        LastSeenAtUtc = SelectedUser.LastSeenAtUtc;
    }

    public async Task<IActionResult> OnPostAsync(long id)
    {
        var u = await _db.Users.SingleOrDefaultAsync(x => x.Id == id);
        if (u is null) return NotFound();

        u.TelegramUserId = TelegramUserId;
        u.Number = string.IsNullOrWhiteSpace(Number) ? null : Number.Trim();
        u.AcquisitionDeepLink = string.IsNullOrWhiteSpace(AcquisitionDeepLink) ? null : AcquisitionDeepLink.Trim();
        u.Balance = decimal.Round(Balance, 2, MidpointRounding.AwayFromZero);
        u.LastSeenAtUtc = LastSeenAtUtc;

        await _db.SaveChangesAsync();
        return RedirectToPage("/Admin/Users/Index", new
        {
            searchPhone = SearchPhone,
            searchTelegramId = SearchTelegramId,
            searchDeepLink = SearchDeepLink,
            sortBy = SortBy,
            sortDir = SortDir,
            pageNumber = PageNumber,
            pageSize = PageSize
        });
    }
}

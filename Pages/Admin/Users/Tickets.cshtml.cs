using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MiniApp.Admin;
using MiniApp.Data;
using MiniApp.Features.Draws;

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
    public List<Draw> Draws { get; private set; } = new();

    [BindProperty]
    public long SelectedDrawId { get; set; }

    [BindProperty]
    public string TicketNumbers { get; set; } = string.Empty;

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

    public string? StatusMessage { get; private set; }
    public bool StatusIsError { get; private set; }

    [TempData]
    public string? FlashMessage { get; set; }

    [TempData]
    public bool? FlashIsError { get; set; }

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(FlashMessage))
        {
            StatusMessage = FlashMessage;
            StatusIsError = FlashIsError ?? false;
        }

        await LoadAsync(id, ct);

        return Page();
    }

    public async Task<IActionResult> OnPostAddTicketAsync(long id, CancellationToken ct)
    {
        var user = await _db.Users.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (user is null)
        {
            FlashMessage = "User was not found.";
            FlashIsError = true;
            return RedirectToPage(new
            {
                id,
                searchPhone = SearchPhone,
                searchTelegramId = SearchTelegramId,
                searchDeepLink = SearchDeepLink,
                sortBy = SortBy,
                sortDir = SortDir,
                pageNumber = PageNumber,
                pageSize = PageSize
            });
        }

        var draw = await _db.Draws.SingleOrDefaultAsync(x => x.Id == SelectedDrawId, ct);
        if (draw is null)
        {
            FlashMessage = "Draw was not found.";
            FlashIsError = true;
            return RedirectToPage(new
            {
                id,
                searchPhone = SearchPhone,
                searchTelegramId = SearchTelegramId,
                searchDeepLink = SearchDeepLink,
                sortBy = SortBy,
                sortDir = SortDir,
                pageNumber = PageNumber,
                pageSize = PageSize
            });
        }

        if (!TryNormalizeSelectedNumbers(TicketNumbers, out var normalizedNumbers, out var signature, out var validationError))
        {
            FlashMessage = validationError;
            FlashIsError = true;
            return RedirectToPage(new
            {
                id,
                searchPhone = SearchPhone,
                searchTelegramId = SearchTelegramId,
                searchDeepLink = SearchDeepLink,
                sortBy = SortBy,
                sortDir = SortDir,
                pageNumber = PageNumber,
                pageSize = PageSize
            });
        }

        var status = draw.State == DrawState.Finished && !string.IsNullOrWhiteSpace(draw.Numbers)
            ? (TicketWinnings.GetMatchCount(normalizedNumbers, draw.Numbers) >= 3
                ? TicketStatus.WinningsAvailable
                : TicketStatus.ExpiredNoWin)
            : TicketStatus.AwaitingDraw;

        _db.Tickets.Add(new Ticket
        {
            UserId = user.Id,
            DrawId = draw.Id,
            Numbers = normalizedNumbers,
            NumbersSignature = signature,
            Status = status,
            PurchasedAtUtc = DateTimeOffset.UtcNow
        });

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("IX_tickets_UserId_DrawId_NumbersSignature", StringComparison.OrdinalIgnoreCase) == true
                                        || ex.Message.Contains("IX_tickets_UserId_DrawId_NumbersSignature", StringComparison.OrdinalIgnoreCase))
        {
            FlashMessage = "This user already has a ticket with the same number set for that draw.";
            FlashIsError = true;
            return RedirectToPage(new { id });
        }

        FlashMessage = $"Ticket added for user #{user.Id} in draw #{draw.Id}.";
        FlashIsError = false;
        return RedirectToPage(new
        {
            id,
            searchPhone = SearchPhone,
            searchTelegramId = SearchTelegramId,
            searchDeepLink = SearchDeepLink,
            sortBy = SortBy,
            sortDir = SortDir,
            pageNumber = PageNumber,
            pageSize = PageSize
        });
    }

    public async Task<IActionResult> OnPostDeleteTicketAsync(long id, long ticketId, CancellationToken ct)
    {
        var t = await _db.Tickets.SingleOrDefaultAsync(x => x.Id == ticketId && x.UserId == id, ct);
        if (t is not null)
        {
            _db.Tickets.Remove(t);
            await _db.SaveChangesAsync(ct);
        }

        return RedirectToPage(new
        {
            id,
            searchPhone = SearchPhone,
            searchTelegramId = SearchTelegramId,
            searchDeepLink = SearchDeepLink,
            sortBy = SortBy,
            sortDir = SortDir,
            pageNumber = PageNumber,
            pageSize = PageSize
        });
    }

    private async Task LoadAsync(long id, CancellationToken ct)
    {
        SelectedUser = await _db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (SelectedUser is null)
            return;

        Draws = await _db.Draws
            .OrderByDescending(x => x.Id)
            .AsNoTracking()
            .ToListAsync(ct);

        if (SelectedDrawId == 0)
        {
            SelectedDrawId = Draws
                .OrderByDescending(x => x.State == DrawState.Active)
                .ThenByDescending(x => x.Id)
                .Select(x => x.Id)
                .FirstOrDefault();
        }

        Tickets = await _db.Tickets
            .Where(x => x.UserId == id)
            .OrderByDescending(x => x.PurchasedAtUtc)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    private static bool TryNormalizeSelectedNumbers(string? rawNumbers, out string normalizedNumbers, out string signature, out string error)
    {
        normalizedNumbers = string.Empty;
        signature = string.Empty;
        error = string.Empty;

        var parts = (rawNumbers ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length != DrawManagement.NumbersPerDraw)
        {
            error = $"Exactly {DrawManagement.NumbersPerDraw} numbers are required.";
            return false;
        }

        var selected = new List<int>(parts.Length);
        var unique = new HashSet<int>();
        foreach (var part in parts)
        {
            if (!int.TryParse(part, out var n))
            {
                error = "All numbers must be valid integers.";
                return false;
            }

            if (n < DrawManagement.MinNumber || n > DrawManagement.MaxNumber)
            {
                error = $"Each number must be between {DrawManagement.MinNumber} and {DrawManagement.MaxNumber}.";
                return false;
            }

            if (!unique.Add(n))
            {
                error = "Numbers must be unique.";
                return false;
            }

            selected.Add(n);
        }

        normalizedNumbers = string.Join(',', selected);
        signature = string.Join(',', selected.OrderBy(x => x));
        return true;
    }
}

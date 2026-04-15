using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MiniApp.Admin;
using MiniApp.Data;
using MiniApp.Features.Draws;
using MiniApp.Features.Localization;

namespace MiniApp.Pages.Admin.Users;

[Authorize(Policy = AdminAuth.PolicyName)]
public sealed class TicketsModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly ILocalizationService _localization;

    public TicketsModel(AppDbContext db, ILocalizationService localization)
    {
        _db = db;
        _localization = localization;
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
    public IReadOnlyDictionary<string, string> UiText { get; private set; } = new Dictionary<string, string>(StringComparer.Ordinal);

    [TempData]
    public string? FlashMessage { get; set; }

    [TempData]
    public bool? FlashIsError { get; set; }

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken ct)
    {
        UiText = await _localization.GetDictionaryAsync(GetAdminLocale(), ct);

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
            FlashMessage = await GetTextAsync("admin.users.tickets.flash.userNotFound", "User was not found.", ct);
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
            FlashMessage = await GetTextAsync("admin.users.tickets.flash.drawNotFound", "Draw was not found.", ct);
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
            FlashMessage = await BuildValidationMessageAsync(validationError, ct);
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
            FlashMessage = await GetTextAsync("admin.users.tickets.flash.duplicate", "This user already has a ticket with the same number set for that draw.", ct);
            FlashIsError = true;
            return RedirectToPage(new { id });
        }

        var successTemplate = await GetTextAsync("admin.users.tickets.flash.added", "Ticket added for user #{0} in draw #{1}.", ct);
        FlashMessage = string.Format(successTemplate, user.Id, draw.Id);
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

    public string Text(string key, string fallback)
        => UiText.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;

    private string GetAdminLocale() => _localization.NormalizeLocale(Request.Cookies["AdminUiLanguage"]);

    private Task<string> GetTextAsync(string key, string fallback, CancellationToken ct)
        => _localization.GetTextAsync(GetAdminLocale(), key, fallback, ct);

    private async Task<string> BuildValidationMessageAsync(string validationCode, CancellationToken ct)
    {
        return validationCode switch
        {
            "exactly_numbers_required" => string.Format(
                await GetTextAsync("admin.users.tickets.validation.exactly_numbers_required", "Exactly {0} numbers are required.", ct),
                DrawManagement.NumbersPerDraw),
            "all_numbers_must_be_integers" => await GetTextAsync(
                "admin.users.tickets.validation.all_numbers_must_be_integers",
                "All numbers must be valid integers.",
                ct),
            "number_out_of_range" => string.Format(
                await GetTextAsync("admin.users.tickets.validation.number_out_of_range", "Each number must be between {0} and {1}.", ct),
                DrawManagement.MinNumber,
                DrawManagement.MaxNumber),
            "numbers_must_be_unique" => await GetTextAsync(
                "admin.users.tickets.validation.numbers_must_be_unique",
                "Numbers must be unique.",
                ct),
            _ => validationCode
        };
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
            error = "exactly_numbers_required";
            return false;
        }

        var selected = new List<int>(parts.Length);
        var unique = new HashSet<int>();
        foreach (var part in parts)
        {
            if (!int.TryParse(part, out var n))
            {
                error = "all_numbers_must_be_integers";
                return false;
            }

            if (n < DrawManagement.MinNumber || n > DrawManagement.MaxNumber)
            {
                error = "number_out_of_range";
                return false;
            }

            if (!unique.Add(n))
            {
                error = "numbers_must_be_unique";
                return false;
            }

            selected.Add(n);
        }

        normalizedNumbers = string.Join(',', selected);
        signature = string.Join(',', selected.OrderBy(x => x));
        return true;
    }
}

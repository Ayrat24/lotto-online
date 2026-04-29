using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniApp.Admin;
using MiniApp.Data;
using MiniApp.Features.Auth;
using MiniApp.Features.Draws;
using MiniApp.Features.Localization;
using MiniApp.Features.Offers;

namespace MiniApp.Pages.Admin.Draws;

[Authorize(Policy = AdminAuth.PolicyName)]
public sealed class EditModel : LocalizedAdminPageModel
{
    public sealed record DrawTicketRow(string Numbers, long SelectionCount);
    public sealed record DrawTicketUserRow(long UserId, long TelegramUserId, string? Number, long TicketCount, DateTimeOffset LastPurchasedAtUtc);
    public sealed record DiscountedOfferRow(long Id, int NumberOfDiscountedTickets, decimal Cost, decimal RegularTotalCost, bool IsActive, bool IsAvailableNow, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc)
    {
        public decimal Savings => decimal.Round(Math.Max(0m, RegularTotalCost - Cost), 2, MidpointRounding.AwayFromZero);
    }

    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public EditModel(AppDbContext db, IConfiguration config, IWebHostEnvironment env, ILocalizationService localization)
        : base(localization)
    {
        _db = db;
        _config = config;
        _env = env;
    }

    public Draw? SelectedDraw { get; private set; }

    public bool CanEdit => SelectedDraw is not null && SelectedDraw.State != DrawState.Finished;
    public IReadOnlyList<string> CardColorOptions => DrawManagement.GetSupportedCardColors();

    public IReadOnlyList<DrawTicketRow> Tickets { get; private set; } = Array.Empty<DrawTicketRow>();
    public IReadOnlyList<DrawTicketUserRow> TicketUsers { get; private set; } = Array.Empty<DrawTicketUserRow>();
    public IReadOnlyList<DiscountedOfferRow> Offers { get; private set; } = Array.Empty<DiscountedOfferRow>();
    public string? SelectedTicketNumbers { get; private set; }
    public int TicketsPage { get; private set; } = 1;
    public int TicketsTotalPages { get; private set; }
    public long TicketsTotalCount { get; private set; }

    public const int TicketsPageSize = 10;

    [BindProperty]
    public decimal PrizePoolMatch3 { get; set; }

    [BindProperty]
    public decimal PrizePoolMatch4 { get; set; }

    [BindProperty]
    public decimal PrizePoolMatch5 { get; set; }

    [BindProperty]
    public decimal TicketCost { get; set; }

    [BindProperty]
    public string CardColor { get; set; } = DrawManagement.DefaultCardColor;

    [BindProperty]
    public string PurchaseClosesAtUtc { get; set; } = string.Empty;

    [BindProperty]
    public string State { get; set; } = "upcoming";

    [BindProperty]
    public string? ExecuteNumbers { get; set; }

    [BindProperty]
    public int OfferNumberOfDiscountedTickets { get; set; } = 1;

    [BindProperty]
    public decimal OfferCost { get; set; }

    public string? StatusMessage { get; private set; }
    public bool StatusIsError { get; private set; }

    [TempData]
    public string? FlashMessage { get; set; }

    [TempData]
    public bool? FlashIsError { get; set; }

    public async Task OnGetAsync(long id, int ticketPage = 1, string? ticketNumbers = null, CancellationToken ct = default)
    {
        await LoadUiTextAsync(ct);
        await EnsureDebugSeedAsync(ct);

        if (!string.IsNullOrWhiteSpace(FlashMessage))
        {
            StatusMessage = FlashMessage;
            StatusIsError = FlashIsError ?? false;
        }

        SelectedDraw = await _db.Draws
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, ct);

        if (SelectedDraw is null)
            return;

        ApplyDrawValues(SelectedDraw);

        if (SelectedDraw.State == DrawState.Finished)
        {
            StatusMessage = await GetTextAsync("admin.draws.edit.flash.finishedReadonly", "Finished draws cannot be edited.", ct);
            StatusIsError = true;
        }

        await SafeLoadTicketsAsync(id, ticketPage, ticketNumbers, ct);
        await LoadOffersAsync(id, ct);
    }

    public async Task<IActionResult> OnPostAsync(long id, int ticketPage = 1, string? ticketNumbers = null, CancellationToken ct = default)
    {
        await LoadUiTextAsync(ct);
        await EnsureDebugSeedAsync(ct);

        var draw = await _db.Draws.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (draw is null)
            return NotFound();

        if (!DrawManagement.TryParseAdminUtcInput(PurchaseClosesAtUtc, out var purchaseClosesAtUtc))
        {
            StatusMessage = await GetTextAsync("admin.draws.flash.invalidPurchaseClosesAtUtc", "Enter a valid UTC purchase close date and time.", ct);
            StatusIsError = true;
            SelectedDraw = draw;
            await SafeLoadTicketsAsync(id, ticketPage, ticketNumbers, ct);
            await LoadOffersAsync(id, ct);
            return Page();
        }

        if (!DrawManagement.TryParseEditableState(State, out var parsedState))
        {
            StatusMessage = await GetTextAsync("admin.draws.edit.flash.invalidState", "State must be active or upcoming.", ct);
            StatusIsError = true;
            SelectedDraw = draw;
            await SafeLoadTicketsAsync(id, ticketPage, ticketNumbers, ct);
            await LoadOffersAsync(id, ct);
            return Page();
        }

        try
        {
            await DrawManagement.UpdateDrawAsync(_db, draw, PrizePoolMatch3, PrizePoolMatch4, PrizePoolMatch5, TicketCost, parsedState, CardColor, purchaseClosesAtUtc, ct);
            var updatedTemplate = await GetTextAsync("admin.draws.edit.flash.updated", "Updated draw #{0}.", ct);
            FlashMessage = string.Format(updatedTemplate, draw.Id);
            FlashIsError = false;
            return RedirectToPage("/Admin/Draws");
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message;
            StatusIsError = true;
            SelectedDraw = draw;
            await SafeLoadTicketsAsync(id, ticketPage, ticketNumbers, ct);
            await LoadOffersAsync(id, ct);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostCreateOfferAsync(long id, int ticketPage = 1, string? ticketNumbers = null, CancellationToken ct = default)
    {
        await LoadUiTextAsync(ct);
        await EnsureDebugSeedAsync(ct);

        var draw = await _db.Draws.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (draw is null)
            return NotFound();

        try
        {
            DiscountedTicketOfferManagement.ValidateForDraw(draw, OfferNumberOfDiscountedTickets, OfferCost);

            var nowUtc = DateTimeOffset.UtcNow;
            _db.DiscountedTicketOffers.Add(new DiscountedTicketOffer
            {
                DrawId = draw.Id,
                NumberOfDiscountedTickets = OfferNumberOfDiscountedTickets,
                Cost = DiscountedTicketOfferManagement.RoundMoney(OfferCost),
                IsActive = true,
                CreatedAtUtc = nowUtc,
                UpdatedAtUtc = nowUtc
            });

            await _db.SaveChangesAsync(ct);
            FlashMessage = await GetTextAsync("admin.draws.edit.offers.flash.created", "Discounted offer created.", ct);
            FlashIsError = false;
            return RedirectToPage(new { id, ticketPage, ticketNumbers });
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message;
            StatusIsError = true;
            SelectedDraw = draw;
            ApplyDrawValues(draw);
            await SafeLoadTicketsAsync(id, ticketPage, ticketNumbers, ct);
            await LoadOffersAsync(id, ct);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostToggleOfferAsync(long id, long offerId, bool isActive, int ticketPage = 1, string? ticketNumbers = null, CancellationToken ct = default)
    {
        await LoadUiTextAsync(ct);
        await EnsureDebugSeedAsync(ct);

        var draw = await _db.Draws.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (draw is null)
            return NotFound();

        var offer = await _db.DiscountedTicketOffers.SingleOrDefaultAsync(x => x.Id == offerId && x.DrawId == id, ct);
        if (offer is null)
        {
            FlashMessage = await GetTextAsync("admin.draws.edit.offers.flash.notFound", "Discounted offer was not found.", ct);
            FlashIsError = true;
            return RedirectToPage(new { id, ticketPage, ticketNumbers });
        }

        try
        {
            if (isActive)
                DiscountedTicketOfferManagement.ValidateForDraw(draw, offer.NumberOfDiscountedTickets, offer.Cost);

            offer.IsActive = isActive;
            offer.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(ct);

            FlashMessage = await GetTextAsync(
                isActive ? "admin.draws.edit.offers.flash.activated" : "admin.draws.edit.offers.flash.deactivated",
                isActive ? "Discounted offer activated." : "Discounted offer deactivated.",
                ct);
            FlashIsError = false;
            return RedirectToPage(new { id, ticketPage, ticketNumbers });
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message;
            StatusIsError = true;
            SelectedDraw = draw;
            ApplyDrawValues(draw);
            await SafeLoadTicketsAsync(id, ticketPage, ticketNumbers, ct);
            await LoadOffersAsync(id, ct);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostExecuteAsync(long id, bool randomize = false, int ticketPage = 1, string? ticketNumbers = null, CancellationToken ct = default)
    {
        await LoadUiTextAsync(ct);
        await EnsureDebugSeedAsync(ct);

        var draw = await _db.Draws.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (draw is null)
            return NotFound();

        try
        {
            var manualNumbers = randomize ? null : ExecuteNumbers;
            await DrawManagement.ExecuteDrawAsync(_db, draw, manualNumbers, ct);
            var executedTemplate = await GetTextAsync("admin.draws.edit.flash.executed", "Executed draw #{0}: {1}.", ct);
            FlashMessage = string.Format(executedTemplate, draw.Id, draw.Numbers);
            FlashIsError = false;
            return RedirectToPage("/Admin/Draws/Result", new { id = draw.Id });
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message;
            StatusIsError = true;
            SelectedDraw = draw;
            ApplyDrawValues(draw);
            await SafeLoadTicketsAsync(id, ticketPage, ticketNumbers, ct);
            await LoadOffersAsync(id, ct);
            return Page();
        }
    }

    private void ApplyDrawValues(Draw draw)
    {
        PrizePoolMatch3 = draw.PrizePoolMatch3;
        PrizePoolMatch4 = draw.PrizePoolMatch4;
        PrizePoolMatch5 = draw.PrizePoolMatch5;
        TicketCost = draw.TicketCost;
        CardColor = DrawManagement.NormalizeCardColor(draw.CardColor);
        PurchaseClosesAtUtc = DrawManagement.FormatAdminUtcInput(draw.PurchaseClosesAtUtc);
        State = DrawManagement.ToStateValue(draw.State);

        if (OfferNumberOfDiscountedTickets < 1)
            OfferNumberOfDiscountedTickets = 1;
        if (OfferCost <= 0)
            OfferCost = draw.TicketCost;
    }

    private async Task LoadOffersAsync(long drawId, CancellationToken ct)
    {
        var nowUtc = DateTimeOffset.UtcNow;
        var draw = SelectedDraw ?? await _db.Draws.AsNoTracking().SingleOrDefaultAsync(x => x.Id == drawId, ct);
        if (draw is null)
        {
            Offers = Array.Empty<DiscountedOfferRow>();
            return;
        }

        var offers = await _db.DiscountedTicketOffers
            .AsNoTracking()
            .Where(x => x.DrawId == drawId)
            .OrderByDescending(x => x.IsActive)
            .ThenByDescending(x => x.UpdatedAtUtc)
            .ThenByDescending(x => x.Id)
            .ToListAsync(ct);

        Offers = offers
            .Select(x => new DiscountedOfferRow(
                x.Id,
                x.NumberOfDiscountedTickets,
                x.Cost,
                DiscountedTicketOfferManagement.GetRegularTotal(draw.TicketCost, x.NumberOfDiscountedTickets),
                x.IsActive,
                DiscountedTicketOfferManagement.IsAvailable(x, draw, nowUtc),
                x.CreatedAtUtc,
                x.UpdatedAtUtc))
            .ToArray();
    }

    private async Task SafeLoadTicketsAsync(long drawId, int requestedPage, string? ticketNumbers, CancellationToken ct)
    {
        try
        {
            await LoadTicketsAsync(drawId, requestedPage, ticketNumbers, ct);
        }
        catch (Exception ex)
        {
            Tickets = Array.Empty<DrawTicketRow>();
            TicketUsers = Array.Empty<DrawTicketUserRow>();
            SelectedTicketNumbers = null;
            TicketsTotalCount = 0;
            TicketsTotalPages = 0;
            TicketsPage = 1;
            var failedTemplate = await GetTextAsync("admin.draws.edit.flash.loadTicketsFailed", "Failed to load draw tickets: {0}", ct);
            StatusMessage = string.Format(failedTemplate, ex.Message);
            StatusIsError = true;
        }
    }

    private async Task LoadTicketsAsync(long drawId, int requestedPage, string? ticketNumbers, CancellationToken ct)
    {
        var ticketsForDraw = _db.Tickets
            .AsNoTracking()
            .Where(x => x.DrawId == drawId);

        var groupedTickets = ticketsForDraw
            .GroupBy(x => x.Numbers)
            .Select(g => new
            {
                Numbers = g.Key,
                SelectionCount = g.LongCount()
            });

        TicketsTotalCount = await groupedTickets.LongCountAsync(ct);

        if (TicketsTotalCount == 0)
        {
            Tickets = Array.Empty<DrawTicketRow>();
            TicketUsers = Array.Empty<DrawTicketUserRow>();
            SelectedTicketNumbers = null;
            TicketsTotalPages = 0;
            TicketsPage = 1;
            return;
        }

        TicketsTotalPages = (int)Math.Ceiling(TicketsTotalCount / (double)TicketsPageSize);
        TicketsPage = Math.Clamp(requestedPage, 1, TicketsTotalPages);

        Tickets = await groupedTickets
            .OrderByDescending(x => x.SelectionCount)
            .ThenBy(x => x.Numbers)
            .Skip((TicketsPage - 1) * TicketsPageSize)
            .Take(TicketsPageSize)
            .Select(x => new DrawTicketRow(x.Numbers, x.SelectionCount))
            .ToArrayAsync(ct);

        SelectedTicketNumbers = string.IsNullOrWhiteSpace(ticketNumbers) ? null : ticketNumbers.Trim();
        if (SelectedTicketNumbers is null)
        {
            TicketUsers = Array.Empty<DrawTicketUserRow>();
            return;
        }

        var ticketUserRows = await _db.Tickets
            .AsNoTracking()
            .Where(x => x.DrawId == drawId && x.Numbers == SelectedTicketNumbers)
            .Select(g => new
            {
                g.UserId,
                g.User.TelegramUserId,
                g.User.Number,
                g.PurchasedAtUtc
            })
            .ToListAsync(ct);

        TicketUsers = ticketUserRows
            .GroupBy(x => new { x.UserId, x.TelegramUserId, x.Number })
            .Select(g => new DrawTicketUserRow(
                g.Key.UserId,
                g.Key.TelegramUserId,
                g.Key.Number,
                g.LongCount(),
                g.Max(x => x.PurchasedAtUtc)))
            .OrderByDescending(x => x.TicketCount)
            .ThenByDescending(x => x.LastPurchasedAtUtc)
            .ThenBy(x => x.UserId)
            .ToArray();
    }

    private async Task EnsureDebugSeedAsync(CancellationToken ct)
    {
        if (!LocalDebugMode.TryGetDebugTelegramUserId(HttpContext, _config, _env, out var debugTelegramUserId))
            return;

        await LocalDebugSeed.EnsureSeededAsync(_db, debugTelegramUserId, ct);
    }
}



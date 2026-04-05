using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MiniApp.Admin;
using MiniApp.Data;
using MiniApp.Features.Auth;
using MiniApp.Features.Draws;

namespace MiniApp.Pages.Admin.Draws;

[Authorize(Policy = AdminAuth.PolicyName)]
public sealed class EditModel : PageModel
{
    public sealed record DrawTicketRow(long Id, string Numbers, DateTimeOffset PurchasedAtUtc, long SelectionCount);

    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public EditModel(AppDbContext db, IConfiguration config, IWebHostEnvironment env)
    {
        _db = db;
        _config = config;
        _env = env;
    }

    public Draw? SelectedDraw { get; private set; }

    public bool CanEdit => SelectedDraw is not null && SelectedDraw.State != DrawState.Finished;

    public IReadOnlyList<DrawTicketRow> Tickets { get; private set; } = Array.Empty<DrawTicketRow>();
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
    public string State { get; set; } = "upcoming";

    public string? StatusMessage { get; private set; }
    public bool StatusIsError { get; private set; }

    [TempData]
    public string? FlashMessage { get; set; }

    [TempData]
    public bool? FlashIsError { get; set; }

    public async Task OnGetAsync(long id, int page = 1, CancellationToken ct = default)
    {
        await EnsureDebugSeedAsync(ct);

        SelectedDraw = await _db.Draws
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, ct);

        if (SelectedDraw is null)
            return;

        PrizePoolMatch3 = SelectedDraw.PrizePoolMatch3;
        PrizePoolMatch4 = SelectedDraw.PrizePoolMatch4;
        PrizePoolMatch5 = SelectedDraw.PrizePoolMatch5;
        State = DrawManagement.ToStateValue(SelectedDraw.State);

        if (SelectedDraw.State == DrawState.Finished)
        {
            StatusMessage = "Finished draws cannot be edited.";
            StatusIsError = true;
        }

        await LoadTicketsAsync(id, page, ct);
    }

    public async Task<IActionResult> OnPostAsync(long id, int page = 1, CancellationToken ct = default)
    {
        await EnsureDebugSeedAsync(ct);

        var draw = await _db.Draws.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (draw is null)
            return NotFound();

        if (!DrawManagement.TryParseEditableState(State, out var parsedState))
        {
            StatusMessage = "State must be active or upcoming.";
            StatusIsError = true;
            SelectedDraw = draw;
            await LoadTicketsAsync(id, page, ct);
            return Page();
        }

        try
        {
            await DrawManagement.UpdateDrawAsync(_db, draw, PrizePoolMatch3, PrizePoolMatch4, PrizePoolMatch5, parsedState, ct);
            FlashMessage = $"Updated draw #{draw.Id}.";
            FlashIsError = false;
            return RedirectToPage("/Admin/Draws");
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message;
            StatusIsError = true;
            SelectedDraw = draw;
            await LoadTicketsAsync(id, page, ct);
            return Page();
        }
    }

    private async Task LoadTicketsAsync(long drawId, int requestedPage, CancellationToken ct)
    {
        var ticketsForDraw = _db.Tickets
            .AsNoTracking()
            .Where(x => x.DrawId == drawId);

        TicketsTotalCount = await ticketsForDraw.LongCountAsync(ct);

        if (TicketsTotalCount == 0)
        {
            Tickets = Array.Empty<DrawTicketRow>();
            TicketsTotalPages = 0;
            TicketsPage = 1;
            return;
        }

        TicketsTotalPages = (int)Math.Ceiling(TicketsTotalCount / (double)TicketsPageSize);
        TicketsPage = Math.Clamp(requestedPage, 1, TicketsTotalPages);

        var ticketCountByNumbers = _db.Tickets
            .AsNoTracking()
            .Where(x => x.DrawId == drawId)
            .GroupBy(x => x.Numbers)
            .Select(g => new { Numbers = g.Key, SelectionCount = g.LongCount() });

        Tickets = await ticketsForDraw
            .Join(
                ticketCountByNumbers,
                ticket => ticket.Numbers,
                grouped => grouped.Numbers,
                (ticket, grouped) => new DrawTicketRow(ticket.Id, ticket.Numbers, ticket.PurchasedAtUtc, grouped.SelectionCount))
            .OrderByDescending(x => x.SelectionCount)
            .ThenByDescending(x => x.PurchasedAtUtc)
            .ThenByDescending(x => x.Id)
            .Skip((TicketsPage - 1) * TicketsPageSize)
            .Take(TicketsPageSize)
            .ToArrayAsync(ct);
    }

    private async Task EnsureDebugSeedAsync(CancellationToken ct)
    {
        if (!LocalDebugMode.TryGetDebugTelegramUserId(HttpContext, _config, _env, out var debugTelegramUserId))
            return;

        await LocalDebugSeed.EnsureSeededAsync(_db, debugTelegramUserId, ct);
    }
}



using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MiniApp.Admin;
using MiniApp.Data;
using MiniApp.Features.Auth;
using MiniApp.Features.Draws;

namespace MiniApp.Pages.Admin;

[Authorize(Policy = AdminAuth.PolicyName)]
public sealed class DrawsModel : PageModel
{
    public sealed record AdminDrawRow(
        long Id,
        decimal PrizePoolMatch3,
        decimal PrizePoolMatch4,
        decimal PrizePoolMatch5,
        string State,
        string? Numbers,
        DateTimeOffset CreatedAtUtc,
        long TicketCount)
    {
        public decimal TotalPrizePool => PrizePoolMatch3 + PrizePoolMatch4 + PrizePoolMatch5;
    }

    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public DrawsModel(AppDbContext db, IConfiguration config, IWebHostEnvironment env)
    {
        _db = db;
        _config = config;
        _env = env;
    }

    public IReadOnlyList<AdminDrawRow> Draws { get; private set; } = Array.Empty<AdminDrawRow>();
    public string? StatusMessage { get; private set; }
    public bool StatusIsError { get; private set; }

    [TempData]
    public string? FlashMessage { get; set; }

    [TempData]
    public bool? FlashIsError { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        await EnsureDebugSeedAsync(ct);
        if (!string.IsNullOrWhiteSpace(FlashMessage))
        {
            StatusMessage = FlashMessage;
            StatusIsError = FlashIsError ?? false;
        }
        await LoadAsync(ct);
    }

    public async Task<IActionResult> OnPostCreateAsync(decimal prizePoolMatch3, decimal prizePoolMatch4, decimal prizePoolMatch5, CancellationToken ct)
    {
        await EnsureDebugSeedAsync(ct);
        try
        {
            var draw = await DrawManagement.CreateDrawAsync(_db, prizePoolMatch3, prizePoolMatch4, prizePoolMatch5, ct);
            StatusMessage = $"Created draw #{draw.Id} in {DrawManagement.ToStateValue(draw.State)} state.";
            StatusIsError = false;
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message;
            StatusIsError = true;
        }

        await LoadAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostExecuteAsync(long id, CancellationToken ct)
    {
        await EnsureDebugSeedAsync(ct);
        var draw = await _db.Draws.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (draw is null)
        {
            StatusMessage = $"Draw #{id} was not found.";
            StatusIsError = true;
            await LoadAsync(ct);
            return Page();
        }

        try
        {
            await DrawManagement.ExecuteDrawAsync(_db, draw, ct);
            StatusMessage = $"Executed draw #{draw.Id}: {draw.Numbers}.";
            StatusIsError = false;
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message;
            StatusIsError = true;
        }

        await LoadAsync(ct);
        return Page();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        var ticketCounts = await _db.Tickets
            .AsNoTracking()
            .GroupBy(x => x.DrawId)
            .Select(g => new
            {
                DrawId = g.Key,
                TicketCount = g.LongCount()
            })
            .ToDictionaryAsync(x => x.DrawId, x => x.TicketCount, ct);

        var draws = await _db.Draws
            .AsNoTracking()
            .OrderByDescending(x => x.Id)
            .Take(200)
            .ToListAsync(ct);

        Draws = draws
            .Select(draw => new AdminDrawRow(
                draw.Id,
                draw.PrizePoolMatch3,
                draw.PrizePoolMatch4,
                draw.PrizePoolMatch5,
                DrawManagement.ToStateValue(draw.State),
                draw.Numbers,
                draw.CreatedAtUtc,
                ticketCounts.GetValueOrDefault(draw.Id)))
            .ToArray();
    }

    private async Task EnsureDebugSeedAsync(CancellationToken ct)
    {
        if (!LocalDebugMode.TryGetDebugTelegramUserId(HttpContext, _config, _env, out var debugTelegramUserId))
            return;

        await LocalDebugSeed.EnsureSeededAsync(_db, debugTelegramUserId, ct);
    }
}

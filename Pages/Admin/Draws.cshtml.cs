using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MiniApp.Admin;
using MiniApp.Data;
using MiniApp.Features.Draws;

namespace MiniApp.Pages.Admin;

[Authorize(Policy = AdminAuth.PolicyName)]
public sealed class DrawsModel : PageModel
{
    public sealed record AdminDrawRow(
        long Id,
        decimal PrizePool,
        string State,
        string? Numbers,
        DateTimeOffset CreatedAtUtc,
        long TicketCount);

    private readonly AppDbContext _db;

    public DrawsModel(AppDbContext db)
    {
        _db = db;
    }

    public IReadOnlyList<AdminDrawRow> Draws { get; private set; } = Array.Empty<AdminDrawRow>();
    public string? StatusMessage { get; private set; }
    public bool StatusIsError { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        await LoadAsync(ct);
    }

    public async Task<IActionResult> OnPostCreateAsync(decimal prizePool, CancellationToken ct)
    {
        try
        {
            var draw = await DrawManagement.CreateDrawAsync(_db, prizePool, ct);
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

    public async Task<IActionResult> OnPostUpdateAsync(long id, decimal prizePool, string state, CancellationToken ct)
    {
        var draw = await _db.Draws.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (draw is null)
        {
            StatusMessage = $"Draw #{id} was not found.";
            StatusIsError = true;
            await LoadAsync(ct);
            return Page();
        }

        if (!DrawManagement.TryParseEditableState(state, out var parsedState))
        {
            StatusMessage = "State must be active or upcoming.";
            StatusIsError = true;
            await LoadAsync(ct);
            return Page();
        }

        try
        {
            await DrawManagement.UpdateDrawAsync(_db, draw, prizePool, parsedState, ct);
            StatusMessage = $"Updated draw #{draw.Id}.";
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
        var ticketCounts = _db.Tickets
            .AsNoTracking()
            .GroupBy(x => x.DrawId)
            .Select(g => new
            {
                DrawId = g.Key,
                TicketCount = g.LongCount()
            });

        Draws = await _db.Draws
            .AsNoTracking()
            .GroupJoin(
                ticketCounts,
                draw => draw.Id,
                ticketCount => ticketCount.DrawId,
                (draw, counts) => new AdminDrawRow(
                    draw.Id,
                    draw.PrizePool,
                    DrawManagement.ToStateValue(draw.State),
                    draw.Numbers,
                    draw.CreatedAtUtc,
                    counts.Select(x => x.TicketCount).FirstOrDefault()))
            .OrderByDescending(x => x.Id)
            .Take(200)
            .ToListAsync(ct);
    }
}

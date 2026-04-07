using Microsoft.EntityFrameworkCore;
using MiniApp.Data;

namespace MiniApp.Features.Draws;

internal static class DrawManagement
{
    public const int NumbersPerDraw = 5;
    public const int MinNumber = 1;
    public const int MaxNumber = 36;

    public static async Task<Draw> CreateDrawAsync(AppDbContext db, decimal prizePoolMatch3, decimal prizePoolMatch4, decimal prizePoolMatch5, decimal ticketCost, CancellationToken ct)
    {
        EnsurePrizePool(prizePoolMatch3, "3/5");
        EnsurePrizePool(prizePoolMatch4, "4/5");
        EnsurePrizePool(prizePoolMatch5, "5/5");
        EnsureTicketCost(ticketCost);

        var nextId = (await db.Draws.MaxAsync(x => (long?)x.Id, ct) ?? 0) + 1;
        var hasActiveDraw = await db.Draws.AnyAsync(x => x.State == DrawState.Active, ct);

        var draw = new Draw
        {
            Id = nextId,
            PrizePoolMatch3 = RoundPrizePool(prizePoolMatch3),
            PrizePoolMatch4 = RoundPrizePool(prizePoolMatch4),
            PrizePoolMatch5 = RoundPrizePool(prizePoolMatch5),
            TicketCost = RoundMoney(ticketCost),
            State = hasActiveDraw ? DrawState.Upcoming : DrawState.Active,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        db.Draws.Add(draw);
        await db.SaveChangesAsync(ct);
        return draw;
    }

    public static async Task UpdateDrawAsync(AppDbContext db, Draw draw, decimal prizePoolMatch3, decimal prizePoolMatch4, decimal prizePoolMatch5, decimal ticketCost, DrawState state, CancellationToken ct)
    {
        EnsurePrizePool(prizePoolMatch3, "3/5");
        EnsurePrizePool(prizePoolMatch4, "4/5");
        EnsurePrizePool(prizePoolMatch5, "5/5");
        EnsureTicketCost(ticketCost);

        if (draw.State == DrawState.Finished)
            throw new InvalidOperationException("Finished draws cannot be edited.");

        if (state == DrawState.Finished)
            throw new InvalidOperationException("Use Execute Draw to finish a draw.");

        if (state == DrawState.Active)
        {
            var activeDraws = await db.Draws
                .Where(x => x.Id != draw.Id && x.State == DrawState.Active)
                .ToListAsync(ct);

            foreach (var activeDraw in activeDraws)
                activeDraw.State = DrawState.Upcoming;
        }

        draw.PrizePoolMatch3 = RoundPrizePool(prizePoolMatch3);
        draw.PrizePoolMatch4 = RoundPrizePool(prizePoolMatch4);
        draw.PrizePoolMatch5 = RoundPrizePool(prizePoolMatch5);
        draw.TicketCost = RoundMoney(ticketCost);
        draw.State = state;
        await db.SaveChangesAsync(ct);
    }

    public static async Task ExecuteDrawAsync(AppDbContext db, Draw draw, string? manualNumbers, CancellationToken ct)
    {
        if (draw.State == DrawState.Finished)
            throw new InvalidOperationException("This draw is already finished.");

        if (draw.State != DrawState.Active)
            throw new InvalidOperationException("Only the active draw can be executed.");

        draw.Numbers = string.IsNullOrWhiteSpace(manualNumbers)
            ? GenerateDrawNumbers()
            : NormalizeManualDrawNumbers(manualNumbers);

        var resultNumbers = ParseNumberSet(draw.Numbers);
        var tickets = await db.Tickets
            .Where(x => x.DrawId == draw.Id)
            .ToListAsync(ct);

        foreach (var ticket in tickets)
        {
            var matchedCount = CountMatches(ticket.Numbers, resultNumbers);
            ticket.Status = matchedCount >= 3
                ? TicketStatus.WinningsAvailable
                : TicketStatus.ExpiredNoWin;
        }

        draw.State = DrawState.Finished;
        await db.SaveChangesAsync(ct);
    }

    public static DrawDto ToDto(Draw draw)
    {
        var totalPrizePool = draw.PrizePoolMatch3 + draw.PrizePoolMatch4 + draw.PrizePoolMatch5;
        return new(
            draw.Id,
            totalPrizePool,
            draw.PrizePoolMatch3,
            draw.PrizePoolMatch4,
            draw.PrizePoolMatch5,
            draw.TicketCost,
            ToStateValue(draw.State),
            draw.Numbers,
            draw.CreatedAtUtc);
    }

    public static bool TryParseEditableState(string? value, out DrawState state)
    {
        if (string.Equals(value, DrawState.Active.ToString(), StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, ToStateValue(DrawState.Active), StringComparison.OrdinalIgnoreCase))
        {
            state = DrawState.Active;
            return true;
        }

        if (string.Equals(value, DrawState.Upcoming.ToString(), StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, ToStateValue(DrawState.Upcoming), StringComparison.OrdinalIgnoreCase))
        {
            state = DrawState.Upcoming;
            return true;
        }

        state = default;
        return false;
    }

    public static string ToStateValue(DrawState state)
        => state switch
        {
            DrawState.Active => "active",
            DrawState.Upcoming => "upcoming",
            DrawState.Finished => "finished",
            _ => state.ToString().ToLowerInvariant()
        };

    public static string ToTicketStatusValue(TicketStatus status)
        => status switch
        {
            TicketStatus.AwaitingDraw => "awaiting_draw",
            TicketStatus.ExpiredNoWin => "expired_no_win",
            TicketStatus.WinningsAvailable => "winnings_available",
            TicketStatus.WinningsClaimed => "winnings_claimed",
            _ => status.ToString().ToLowerInvariant()
        };

    public static string GenerateDrawNumbers()
    {
        var set = new HashSet<int>();
        while (set.Count < NumbersPerDraw)
            set.Add(Random.Shared.Next(MinNumber, MaxNumber + 1));

        var arr = set.ToArray();
        Array.Sort(arr);
        return string.Join(',', arr);
    }

    private static string NormalizeManualDrawNumbers(string manualNumbers)
    {
        var parts = manualNumbers
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();

        if (parts.Length != NumbersPerDraw)
            throw new InvalidOperationException($"Provide exactly {NumbersPerDraw} numbers separated by commas.");

        var set = new HashSet<int>();
        foreach (var part in parts)
        {
            if (!int.TryParse(part, out var value))
                throw new InvalidOperationException("Manual draw numbers must be valid integers.");

            if (value < MinNumber || value > MaxNumber)
                throw new InvalidOperationException($"Each draw number must be between {MinNumber} and {MaxNumber}.");

            if (!set.Add(value))
                throw new InvalidOperationException("Manual draw numbers must be unique.");
        }

        var arr = set.ToArray();
        Array.Sort(arr);
        return string.Join(',', arr);
    }

    private static decimal RoundPrizePool(decimal prizePool)
        => decimal.Round(prizePool, 2, MidpointRounding.AwayFromZero);

    private static decimal RoundMoney(decimal value)
        => decimal.Round(value, 2, MidpointRounding.AwayFromZero);

    private static HashSet<int> ParseNumberSet(string numbers)
    {
        var values = numbers
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(int.Parse)
            .ToHashSet();

        return values;
    }

    private static int CountMatches(string ticketNumbers, HashSet<int> drawNumbers)
    {
        var ticketValues = ParseNumberSet(ticketNumbers);

        var matches = 0;
        foreach (var n in ticketValues)
        {
            if (drawNumbers.Contains(n))
                matches++;
        }

        return matches;
    }

    private static void EnsurePrizePool(decimal prizePool, string tier)
    {
        if (prizePool < 0)
            throw new InvalidOperationException($"Prize pool for {tier} cannot be negative.");
    }

    private static void EnsureTicketCost(decimal ticketCost)
    {
        if (ticketCost <= 0)
            throw new InvalidOperationException("Ticket cost must be greater than zero.");
    }
}


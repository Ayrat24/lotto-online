using System.Globalization;
using Microsoft.EntityFrameworkCore;
using MiniApp.Data;

namespace MiniApp.Features.Draws;

internal static class DrawManagement
{
    public const int NumbersPerDraw = 5;
    public const int MinNumber = 1;
    public const int MaxNumber = 36;

    public static async Task<Draw> CreateDrawAsync(AppDbContext db, decimal prizePoolMatch3, decimal prizePoolMatch4, decimal prizePoolMatch5, decimal ticketCost, DateTimeOffset? purchaseClosesAtUtc, CancellationToken ct)
    {
        EnsurePrizePool(prizePoolMatch3, "3/5");
        EnsurePrizePool(prizePoolMatch4, "4/5");
        EnsurePrizePool(prizePoolMatch5, "5/5");
        EnsureTicketCost(ticketCost);

        var nowUtc = DateTimeOffset.UtcNow;
        var nextId = (await db.Draws.MaxAsync(x => (long?)x.Id, ct) ?? 0) + 1;
        var draw = new Draw
        {
            Id = nextId,
            PrizePoolMatch3 = RoundPrizePool(prizePoolMatch3),
            PrizePoolMatch4 = RoundPrizePool(prizePoolMatch4),
            PrizePoolMatch5 = RoundPrizePool(prizePoolMatch5),
            TicketCost = RoundMoney(ticketCost),
            State = DrawState.Active,
            CreatedAtUtc = nowUtc,
            PurchaseClosesAtUtc = NormalizePurchaseClosesAtUtc(purchaseClosesAtUtc, GetDefaultPurchaseClosesAtUtc(nowUtc))
        };

        db.Draws.Add(draw);
        await db.SaveChangesAsync(ct);
        return draw;
    }

    public static async Task UpdateDrawAsync(AppDbContext db, Draw draw, decimal prizePoolMatch3, decimal prizePoolMatch4, decimal prizePoolMatch5, decimal ticketCost, DrawState state, DateTimeOffset? purchaseClosesAtUtc, CancellationToken ct)
    {
        EnsurePrizePool(prizePoolMatch3, "3/5");
        EnsurePrizePool(prizePoolMatch4, "4/5");
        EnsurePrizePool(prizePoolMatch5, "5/5");
        EnsureTicketCost(ticketCost);

        if (draw.State == DrawState.Finished)
            throw new InvalidOperationException("Finished draws cannot be edited.");

        if (state == DrawState.Finished)
            throw new InvalidOperationException("Use Execute Draw to finish a draw.");


        draw.PrizePoolMatch3 = RoundPrizePool(prizePoolMatch3);
        draw.PrizePoolMatch4 = RoundPrizePool(prizePoolMatch4);
        draw.PrizePoolMatch5 = RoundPrizePool(prizePoolMatch5);
        draw.TicketCost = RoundMoney(ticketCost);
        draw.State = state;
        draw.PurchaseClosesAtUtc = NormalizePurchaseClosesAtUtc(purchaseClosesAtUtc, draw.PurchaseClosesAtUtc);
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

        var tickets = await db.Tickets
            .Where(x => x.DrawId == draw.Id)
            .ToListAsync(ct);

        foreach (var ticket in tickets)
        {
            var matchedCount = TicketWinnings.GetMatchCount(ticket.Numbers, draw.Numbers);
            ticket.Status = matchedCount >= 3
                ? TicketStatus.WinningsAvailable
                : TicketStatus.ExpiredNoWin;
        }

        draw.State = DrawState.Finished;
        await db.SaveChangesAsync(ct);
    }

    public static DrawDto ToDto(Draw draw, DateTimeOffset? nowUtc = null)
    {
        var effectiveNowUtc = nowUtc?.ToUniversalTime() ?? DateTimeOffset.UtcNow;
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
            draw.CreatedAtUtc,
            draw.PurchaseClosesAtUtc,
            CanPurchase(draw, effectiveNowUtc));
    }

    public static bool CanPurchase(Draw draw, DateTimeOffset nowUtc)
        => draw.State == DrawState.Active && draw.PurchaseClosesAtUtc > nowUtc.ToUniversalTime();

    public static DateTimeOffset GetDefaultPurchaseClosesAtUtc(DateTimeOffset nowUtc)
        => nowUtc.ToUniversalTime().AddHours(1);

    public static string FormatAdminUtcInput(DateTimeOffset value)
        => value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);

    public static bool TryParseAdminUtcInput(string? value, out DateTimeOffset purchaseClosesAtUtc)
    {
        if (DateTime.TryParseExact(
            value,
            ["yyyy-MM-ddTHH:mm", "yyyy-MM-ddTHH:mm:ss"],
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed))
        {
            purchaseClosesAtUtc = new DateTimeOffset(DateTime.SpecifyKind(parsed, DateTimeKind.Utc));
            return true;
        }

        purchaseClosesAtUtc = default;
        return false;
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
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

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

    private static DateTimeOffset NormalizePurchaseClosesAtUtc(DateTimeOffset? purchaseClosesAtUtc, DateTimeOffset fallbackUtc)
        => purchaseClosesAtUtc?.ToUniversalTime() ?? fallbackUtc.ToUniversalTime();


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


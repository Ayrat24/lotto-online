using Microsoft.EntityFrameworkCore;
using MiniApp.Data;

namespace MiniApp.Features.Draws;

internal static class DrawManagement
{
    public const int NumbersPerDraw = 5;
    public const int MinNumber = 1;
    public const int MaxNumber = 36;

    public static async Task<Draw> CreateDrawAsync(AppDbContext db, decimal prizePool, CancellationToken ct)
    {
        EnsurePrizePool(prizePool);

        var nextId = (await db.Draws.MaxAsync(x => (long?)x.Id, ct) ?? 0) + 1;
        var hasActiveDraw = await db.Draws.AnyAsync(x => x.State == DrawState.Active, ct);

        var draw = new Draw
        {
            Id = nextId,
            PrizePool = decimal.Round(prizePool, 2, MidpointRounding.AwayFromZero),
            State = hasActiveDraw ? DrawState.Upcoming : DrawState.Active,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        db.Draws.Add(draw);
        await db.SaveChangesAsync(ct);
        return draw;
    }

    public static async Task UpdateDrawAsync(AppDbContext db, Draw draw, decimal prizePool, DrawState state, CancellationToken ct)
    {
        EnsurePrizePool(prizePool);

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

        draw.PrizePool = decimal.Round(prizePool, 2, MidpointRounding.AwayFromZero);
        draw.State = state;
        await db.SaveChangesAsync(ct);
    }

    public static async Task ExecuteDrawAsync(AppDbContext db, Draw draw, CancellationToken ct)
    {
        if (draw.State == DrawState.Finished)
            throw new InvalidOperationException("This draw is already finished.");

        if (draw.State != DrawState.Active)
            throw new InvalidOperationException("Only the active draw can be executed.");

        draw.Numbers = GenerateDrawNumbers();
        draw.State = DrawState.Finished;
        await db.SaveChangesAsync(ct);
    }

    public static DrawDto ToDto(Draw draw)
        => new(draw.Id, draw.PrizePool, ToStateValue(draw.State), draw.Numbers, draw.CreatedAtUtc);

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

    public static string GenerateDrawNumbers()
    {
        var set = new HashSet<int>();
        while (set.Count < NumbersPerDraw)
            set.Add(Random.Shared.Next(MinNumber, MaxNumber + 1));

        var arr = set.ToArray();
        Array.Sort(arr);
        return string.Join(',', arr);
    }

    private static void EnsurePrizePool(decimal prizePool)
    {
        if (prizePool < 0)
            throw new InvalidOperationException("Prize pool cannot be negative.");
    }
}


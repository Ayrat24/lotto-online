using Microsoft.EntityFrameworkCore;
using MiniApp.Data;
using MiniApp.Features.Draws;

namespace MiniApp.Features.Auth;

public static class LocalDebugSeed
{
    private const long FakeUserA = 700000001;
    private const long FakeUserB = 700000002;

    public static async Task EnsureSeededAsync(AppDbContext db, long debugTelegramUserId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        await EnsureUserAsync(db, debugTelegramUserId, "+10000000001", now, ct);
        await EnsureUserAsync(db, FakeUserA, "+10000000002", now, ct);
        await EnsureUserAsync(db, FakeUserB, "+10000000003", now, ct);

        var debugUser = await db.Users.SingleAsync(x => x.TelegramUserId == debugTelegramUserId, ct);

        var draws = await db.Draws
            .OrderBy(x => x.Id)
            .ToListAsync(ct);

        long nextId = draws.Count == 0 ? 1 : draws[^1].Id + 1;

        var finishedDraws = draws.Where(x => x.State == DrawState.Finished).OrderByDescending(x => x.Id).ToList();
        while (finishedDraws.Count < 2)
        {
            var finished = new Draw
            {
                Id = nextId++,
                PrizePool = 100m + finishedDraws.Count * 50m,
                State = DrawState.Finished,
                Numbers = DrawManagement.GenerateDrawNumbers(),
                CreatedAtUtc = now.AddHours(-3 + finishedDraws.Count)
            };

            db.Draws.Add(finished);
            draws.Add(finished);
            finishedDraws.Add(finished);
        }

        var activeDraw = draws
            .Where(x => x.State == DrawState.Active)
            .OrderByDescending(x => x.Id)
            .FirstOrDefault();

        if (activeDraw is null)
        {
            activeDraw = new Draw
            {
                Id = nextId++,
                PrizePool = 200m,
                State = DrawState.Active,
                CreatedAtUtc = now.AddHours(-1)
            };
            db.Draws.Add(activeDraw);
            draws.Add(activeDraw);
        }

        var otherActiveDraws = draws.Where(x => x.State == DrawState.Active && x.Id != activeDraw.Id).ToList();
        foreach (var d in otherActiveDraws)
            d.State = DrawState.Upcoming;

        var upcomingDraw = draws
            .Where(x => x.State == DrawState.Upcoming)
            .OrderByDescending(x => x.Id)
            .FirstOrDefault();

        if (upcomingDraw is null)
        {
            upcomingDraw = new Draw
            {
                Id = nextId++,
                PrizePool = 300m,
                State = DrawState.Upcoming,
                CreatedAtUtc = now
            };
            db.Draws.Add(upcomingDraw);
            draws.Add(upcomingDraw);
        }

        await db.SaveChangesAsync(ct);

        var finishedIds = finishedDraws
            .OrderByDescending(x => x.Id)
            .Take(2)
            .Select(x => x.Id)
            .ToArray();

        foreach (var finishedId in finishedIds)
        {
            var hasTicket = await db.Tickets.AnyAsync(x => x.UserId == debugUser.Id && x.DrawId == finishedId, ct);
            if (hasTicket)
                continue;

            db.Tickets.Add(new Ticket
            {
                UserId = debugUser.Id,
                DrawId = finishedId,
                Numbers = DrawManagement.GenerateDrawNumbers(),
                PurchasedAtUtc = now.AddMinutes(-Random.Shared.Next(10, 180))
            });
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureUserAsync(AppDbContext db, long telegramUserId, string number, DateTimeOffset now, CancellationToken ct)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.TelegramUserId == telegramUserId, ct);
        if (user is not null)
            return;

        db.Users.Add(new MiniAppUser
        {
            TelegramUserId = telegramUserId,
            Number = number,
            CreatedAtUtc = now,
            LastSeenAtUtc = now
        });

        await db.SaveChangesAsync(ct);
    }
}




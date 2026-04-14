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

        await EnsureUserAsync(db, debugTelegramUserId, "+10000000001", 100m, now, ct);
        await EnsureUserAsync(db, FakeUserA, "+10000000002", 25m, now, ct);
        await EnsureUserAsync(db, FakeUserB, "+10000000003", 25m, now, ct);

        var debugUser = await db.Users.SingleAsync(x => x.TelegramUserId == debugTelegramUserId, ct);

        var draws = await db.Draws
            .OrderBy(x => x.Id)
            .ToListAsync(ct);

        foreach (var draw in draws)
        {
            if (draw.TicketCost <= 0)
                draw.TicketCost = 2m;
        }

        long nextId = draws.Count == 0 ? 1 : draws[^1].Id + 1;

        var finishedDraws = draws.Where(x => x.State == DrawState.Finished).OrderByDescending(x => x.Id).ToList();
        while (finishedDraws.Count < 2)
        {
            var finished = new Draw
            {
                Id = nextId++,
                PrizePoolMatch3 = 40m + finishedDraws.Count * 10m,
                PrizePoolMatch4 = 25m + finishedDraws.Count * 10m,
                PrizePoolMatch5 = 35m + finishedDraws.Count * 30m,
                TicketCost = 2m,
                State = DrawState.Finished,
                Numbers = DrawManagement.GenerateDrawNumbers(),
                CreatedAtUtc = now.AddHours(-3 + finishedDraws.Count)
            };

            db.Draws.Add(finished);
            draws.Add(finished);
            finishedDraws.Add(finished);
        }

        var targetActiveDrawCount = GetTargetActiveDrawCount(debugTelegramUserId, now);
        var activeDraws = draws
            .Where(x => x.State == DrawState.Active)
            .OrderByDescending(x => x.Id)
            .ToList();

        if (activeDraws.Count > targetActiveDrawCount)
        {
            foreach (var drawToDemote in activeDraws.Skip(targetActiveDrawCount))
                drawToDemote.State = DrawState.Upcoming;

            activeDraws = activeDraws.Take(targetActiveDrawCount).ToList();
        }

        if (activeDraws.Count < targetActiveDrawCount)
        {
            var upcomingPool = draws
                .Where(x => x.State == DrawState.Upcoming)
                .OrderByDescending(x => x.Id)
                .ToList();

            while (activeDraws.Count < targetActiveDrawCount)
            {
                Draw drawToActivate;
                if (upcomingPool.Count > 0)
                {
                    drawToActivate = upcomingPool[0];
                    upcomingPool.RemoveAt(0);
                }
                else
                {
                    drawToActivate = new Draw
                    {
                        Id = nextId++,
                        PrizePoolMatch3 = 80m + activeDraws.Count * 20m,
                        PrizePoolMatch4 = 50m + activeDraws.Count * 20m,
                        PrizePoolMatch5 = 70m + activeDraws.Count * 30m,
                        TicketCost = 2m,
                        State = DrawState.Active,
                        CreatedAtUtc = now.AddHours(-1).AddMinutes(-20 * activeDraws.Count)
                    };

                    db.Draws.Add(drawToActivate);
                    draws.Add(drawToActivate);
                }

                drawToActivate.State = DrawState.Active;
                activeDraws.Add(drawToActivate);
            }
        }

        var upcomingDraw = draws
            .Where(x => x.State == DrawState.Upcoming)
            .OrderByDescending(x => x.Id)
            .FirstOrDefault();

        if (upcomingDraw is null)
        {
            upcomingDraw = new Draw
            {
                Id = nextId++,
                PrizePoolMatch3 = 120m,
                PrizePoolMatch4 = 70m,
                PrizePoolMatch5 = 110m,
                TicketCost = 2m,
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
                Status = TicketStatus.AwaitingDraw,
                PurchasedAtUtc = now.AddMinutes(-Random.Shared.Next(10, 180))
            });
        }

        var allTickets = await db.Tickets
            .Where(x => x.UserId == debugUser.Id)
            .ToListAsync(ct);

        var drawsById = draws.ToDictionary(x => x.Id);
        foreach (var ticket in allTickets)
        {
            if (!drawsById.TryGetValue(ticket.DrawId, out var drawForTicket))
                continue;

            if (drawForTicket.State != DrawState.Finished || string.IsNullOrWhiteSpace(drawForTicket.Numbers))
            {
                ticket.Status = TicketStatus.AwaitingDraw;
                continue;
            }

            var matchCount = CountMatches(ticket.Numbers, drawForTicket.Numbers);
            ticket.Status = matchCount >= 3
                ? TicketStatus.WinningsAvailable
                : TicketStatus.ExpiredNoWin;
        }

        await db.SaveChangesAsync(ct);
    }

    private static int GetTargetActiveDrawCount(long debugTelegramUserId, DateTimeOffset now)
    {
        // Keep randomization stable throughout the day to avoid draw state flapping during polling.
        var dayOfYear = now.UtcDateTime.DayOfYear;
        var year = now.UtcDateTime.Year;
        var seed = HashCode.Combine(debugTelegramUserId, year, dayOfYear);
        var rng = new Random(seed);
        return rng.Next(1, 3);
    }

    private static int CountMatches(string ticketNumbers, string drawNumbers)
    {
        var drawSet = drawNumbers
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(int.Parse)
            .ToHashSet();

        var ticketSet = ticketNumbers
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(int.Parse)
            .ToHashSet();

        var matches = 0;
        foreach (var n in ticketSet)
        {
            if (drawSet.Contains(n))
                matches++;
        }

        return matches;
    }

    private static async Task EnsureUserAsync(AppDbContext db, long telegramUserId, string number, decimal minBalance, DateTimeOffset now, CancellationToken ct)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.TelegramUserId == telegramUserId, ct);
        if (user is not null)
        {
            user.PreferredLanguage ??= "en";
            if (user.Balance < minBalance)
            {
                user.Balance = minBalance;
                await db.SaveChangesAsync(ct);
                return;
            }

            await db.SaveChangesAsync(ct);
            return;
        }

        db.Users.Add(new MiniAppUser
        {
            TelegramUserId = telegramUserId,
            Number = number,
            PreferredLanguage = "en",
            ReferredByUserIdOrUnbound = MiniAppUser.UnboundReferralUserId,
            Balance = minBalance,
            CreatedAtUtc = now,
            LastSeenAtUtc = now
        });

        await db.SaveChangesAsync(ct);
    }
}




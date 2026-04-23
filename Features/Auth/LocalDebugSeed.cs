using Microsoft.EntityFrameworkCore;
using MiniApp.Data;
using MiniApp.Features.Draws;
using System.Globalization;

namespace MiniApp.Features.Auth;

public static class LocalDebugSeed
{
    private sealed record DebugUserSeed(
        long TelegramUserId,
        string Number,
        decimal MinBalance,
        string PreferredLanguage,
        bool IsFake,
        string AcquisitionDeepLink,
        int LastSeenMinutesAgo);

    public static async Task EnsureSeededAsync(AppDbContext db, long debugTelegramUserId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        var seeds = new[]
        {
            new DebugUserSeed(debugTelegramUserId, "+10000000001", 100m, "en", false, "local-debug-primary", 1),
            new DebugUserSeed(700000001, "+10000000002", 25m, "en", true, "local-debug-seed-a", 6),
            new DebugUserSeed(700000002, "+10000000003", 25m, "ru", true, "local-debug-seed-b", 11),
            new DebugUserSeed(700000003, "+10000000004", 40m, "uz", true, "local-debug-seed-c", 18),
            new DebugUserSeed(700000004, "+10000000005", 75m, "en", true, "local-debug-seed-d", 27),
            new DebugUserSeed(700000005, "+10000000006", 10m, "ru", true, "local-debug-seed-e", 34),
            new DebugUserSeed(700000006, "+10000000007", 60m, "uz", true, "local-debug-seed-f", 49)
        };

        foreach (var seed in seeds)
            await EnsureUserAsync(db, seed, now, ct);

        await EnsureWinnerEntriesAsync(db, now, ct);

        var debugUser = await db.Users.SingleAsync(x => x.TelegramUserId == debugTelegramUserId, ct);

        var draws = await db.Draws
            .OrderBy(x => x.Id)
            .ToListAsync(ct);

        foreach (var draw in draws)
        {
            if (draw.TicketCost <= 0)
                draw.TicketCost = 2m;

            if (string.IsNullOrWhiteSpace(draw.CardColor))
                draw.CardColor = GetSeedCardColor((int)(draw.Id % 5));
        }

        long nextId = draws.Count == 0 ? 1 : draws[^1].Id + 1;

        var finishedDraws = draws.Where(x => x.State == DrawState.Finished).OrderByDescending(x => x.Id).ToList();
        while (finishedDraws.Count < 2)
        {
            var finished = new Draw
            {
                Id = nextId++,
                CardColor = GetSeedCardColor(finishedDraws.Count + 2),
                PrizePoolMatch3 = 40m + finishedDraws.Count * 10m,
                PrizePoolMatch4 = 25m + finishedDraws.Count * 10m,
                PrizePoolMatch5 = 35m + finishedDraws.Count * 30m,
                TicketCost = 2m,
                State = DrawState.Finished,
                Numbers = DrawManagement.GenerateDrawNumbers(),
                CreatedAtUtc = now.AddHours(-3 + finishedDraws.Count),
                PurchaseClosesAtUtc = now.AddHours(-2 + finishedDraws.Count)
            };

            db.Draws.Add(finished);
            draws.Add(finished);
            finishedDraws.Add(finished);
        }

        var targetActiveDrawCount = GetTargetActiveDrawCount();
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
                        CardColor = GetSeedCardColor(activeDraws.Count),
                        PrizePoolMatch3 = 80m + activeDraws.Count * 20m,
                        PrizePoolMatch4 = 50m + activeDraws.Count * 20m,
                        PrizePoolMatch5 = 70m + activeDraws.Count * 30m,
                        TicketCost = 2m,
                        State = DrawState.Active,
                        CreatedAtUtc = now.AddHours(-1).AddMinutes(-20 * activeDraws.Count),
                        PurchaseClosesAtUtc = now.AddMinutes(45 + (activeDraws.Count * 20))
                    };

                    db.Draws.Add(drawToActivate);
                    draws.Add(drawToActivate);
                }

                drawToActivate.State = DrawState.Active;
                drawToActivate.CardColor = GetSeedCardColor(activeDraws.Count);
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
                Id = nextId,
                CardColor = GetSeedCardColor(4),
                PrizePoolMatch3 = 120m,
                PrizePoolMatch4 = 70m,
                PrizePoolMatch5 = 110m,
                TicketCost = 2m,
                State = DrawState.Upcoming,
                CreatedAtUtc = now,
                PurchaseClosesAtUtc = now.AddHours(3)
            };
            nextId++;
            db.Draws.Add(upcomingDraw);
            draws.Add(upcomingDraw);
        }

        for (var i = 0; i < activeDraws.Count; i++)
        {
            activeDraws[i].PurchaseClosesAtUtc = now.AddMinutes(45 + (i * 20));
        }

        foreach (var draw in draws.Where(x => x.State == DrawState.Upcoming))
        {
            if (draw.PurchaseClosesAtUtc <= now)
                draw.PurchaseClosesAtUtc = now.AddHours(3);
        }

        foreach (var draw in draws.Where(x => x.State == DrawState.Finished))
        {
            if (draw.PurchaseClosesAtUtc > now)
                draw.PurchaseClosesAtUtc = draw.CreatedAtUtc.AddHours(1);
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

    private static int GetTargetActiveDrawCount()
    {
        // Keep debug UX deterministic: always have at least two active draws to exercise multi-draw UI.
        return 2;
    }

    private static string GetSeedCardColor(int index)
    {
        var colors = DrawManagement.GetSupportedCardColors();
        if (colors.Count == 0)
            return DrawManagement.DefaultCardColor;

        var normalizedIndex = Math.Abs(index) % colors.Count;
        return colors[normalizedIndex];
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

    private static async Task EnsureUserAsync(AppDbContext db, DebugUserSeed seed, DateTimeOffset now, CancellationToken ct)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.TelegramUserId == seed.TelegramUserId, ct);
        if (user is not null)
        {
            user.Number ??= seed.Number;
            user.PreferredLanguage ??= seed.PreferredLanguage;
            user.AcquisitionDeepLink ??= seed.AcquisitionDeepLink;
            user.InviteCode ??= BuildDeterministicInviteCode(seed.TelegramUserId);
            if (seed.IsFake)
                user.IsFake = true;
            if (user.Balance < seed.MinBalance)
                user.Balance = seed.MinBalance;
            if (user.LastSeenAtUtc < now.AddMinutes(-seed.LastSeenMinutesAgo))
                user.LastSeenAtUtc = now.AddMinutes(-seed.LastSeenMinutesAgo);

            await db.SaveChangesAsync(ct);
            return;
        }

        db.Users.Add(new MiniAppUser
        {
            TelegramUserId = seed.TelegramUserId,
            Number = seed.Number,
            PreferredLanguage = seed.PreferredLanguage,
            AcquisitionDeepLink = seed.AcquisitionDeepLink,
            InviteCode = BuildDeterministicInviteCode(seed.TelegramUserId),
            ReferredByUserIdOrUnbound = MiniAppUser.UnboundReferralUserId,
            IsFake = seed.IsFake,
            Balance = seed.MinBalance,
            CreatedAtUtc = now,
            LastSeenAtUtc = now.AddMinutes(-seed.LastSeenMinutesAgo)
        });

        await db.SaveChangesAsync(ct);
    }

    private static string BuildDeterministicInviteCode(long telegramUserId)
        => $"D{Math.Abs(telegramUserId).ToString(CultureInfo.InvariantCulture).PadLeft(9, '0')}";

    private static async Task EnsureWinnerEntriesAsync(AppDbContext db, DateTimeOffset now, CancellationToken ct)
    {
        var hasPublishedWinners = await db.Set<WinnerEntry>()
            .AsNoTracking()
            .AnyAsync(x => x.IsPublished, ct);

        if (hasPublishedWinners)
            return;

        db.Set<WinnerEntry>().AddRange(
            new WinnerEntry
            {
                Name = "Ludmila Boltenkova",
                WinningAmountText = "1 000 000 rub.",
                QuoteText = "Winning felt like a gift for me.",
                PhotoPath = "/img/debug-winners/ludmila.svg",
                DisplayOrder = 0,
                IsPublished = true,
                CreatedAtUtc = now.AddMinutes(-12),
                UpdatedAtUtc = now.AddMinutes(-12)
            },
            new WinnerEntry
            {
                Name = "Lidia M.",
                WinningAmountText = "38 870 613 rub.",
                QuoteText = "At first I thought I had won only 38,000 rubles.",
                PhotoPath = "/img/debug-winners/lidia.svg",
                DisplayOrder = 1,
                IsPublished = true,
                CreatedAtUtc = now.AddMinutes(-11),
                UpdatedAtUtc = now.AddMinutes(-11)
            },
            new WinnerEntry
            {
                Name = "Svetlana Tolkova",
                WinningAmountText = "2 500 000 rub.",
                QuoteText = "The day before the win, my left palm would not stop itching.",
                PhotoPath = "/img/debug-winners/svetlana.svg",
                DisplayOrder = 2,
                IsPublished = true,
                CreatedAtUtc = now.AddMinutes(-10),
                UpdatedAtUtc = now.AddMinutes(-10)
            });

        await db.SaveChangesAsync(ct);
    }
}




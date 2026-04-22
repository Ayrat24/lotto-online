using Microsoft.EntityFrameworkCore;
using MiniApp.Data;
using MiniApp.Features.Auth;
using MiniApp.Features.Draws;
using MiniApp.TelegramLogin;

namespace MiniApp.Features.Timeline;

public static class TimelineEndpoints
{
    public static IEndpointRouteBuilder MapTimelineEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Returns the current draw, the user's tickets for it, and grouped personal history for previous draws.
        endpoints.MapPost("/api/timeline", async (
            TimelineRequest req,
            HttpContext http,
            IConfiguration config,
            IWebHostEnvironment env,
            AppDbContext db,
            ITicketPurchaseSettingsService ticketPurchaseSettings,
            CancellationToken ct) =>
        {
            long telegramUserId;

            if (LocalDebugMode.TryGetDebugTelegramUserId(http, config, env, out var localDebugUserId))
            {
                await LocalDebugSeed.EnsureSeededAsync(db, localDebugUserId, ct);
                telegramUserId = localDebugUserId;
            }
            else
            {
                var botToken = config["BotToken"];
                if (string.IsNullOrWhiteSpace(botToken))
                    return Results.Problem("BotToken is not configured.", statusCode: 500);

                if (!TelegramInitDataValidator.TryValidateInitData(req.InitData, botToken, TimeSpan.FromMinutes(10), out var tgUser, out var error))
                {
                    if (env.IsDevelopment())
                        return Results.Json(new { ok = false, error }, statusCode: StatusCodes.Status401Unauthorized);
                    return Results.Unauthorized();
                }

                telegramUserId = tgUser!.Id;
            }

            var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.TelegramUserId == telegramUserId, ct);

            var activeDrawEntities = await db.Draws
                .Where(x => x.State == DrawState.Active)
                .OrderByDescending(x => x.Id)
                .AsNoTracking()
                .ToListAsync(ct);

            var activeDrawEntity = activeDrawEntities.FirstOrDefault();

            Draw? featuredDrawEntity = activeDrawEntity;
            if (featuredDrawEntity is null)
            {
                if (user is not null)
                {
                    var winningDrawId = await db.Tickets
                        .AsNoTracking()
                        .Where(x => x.UserId == user.Id && x.Status == TicketStatus.WinningsAvailable)
                        .Select(x => (long?)x.DrawId)
                        .MaxAsync(ct);

                    if (winningDrawId.HasValue)
                    {
                        featuredDrawEntity = await db.Draws
                            .Where(x => x.Id == winningDrawId.Value && x.State == DrawState.Finished)
                            .AsNoTracking()
                            .FirstOrDefaultAsync(ct);
                    }
                }

                featuredDrawEntity ??= await db.Draws
                    .Where(x => x.State == DrawState.Finished)
                    .OrderByDescending(x => x.Id)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(ct);
            }

            var currentDraw = featuredDrawEntity is null
                ? null
                : DrawManagement.ToDto(featuredDrawEntity);

            var activeDraws = activeDrawEntities
                .Select(DrawManagement.ToDto)
                .ToArray();

            var ticketRows = user is null
                ? new List<Ticket>()
                : await db.Tickets
                    .Where(x => x.UserId == user.Id)
                    .OrderByDescending(x => x.PurchasedAtUtc)
                    .AsNoTracking()
                    .ToListAsync(ct);

            var ticketDrawIds = ticketRows.Select(x => x.DrawId).Distinct().ToArray();
            var ticketDrawsById = ticketDrawIds.Length == 0
                ? new Dictionary<long, Draw>()
                : await db.Draws
                    .Where(x => ticketDrawIds.Contains(x.Id))
                    .AsNoTracking()
                    .ToDictionaryAsync(x => x.Id, ct);

            var tickets = ticketRows
                .Select(x =>
                {
                    ticketDrawsById.TryGetValue(x.DrawId, out var draw);
                    var winningAmount = draw is null ? 0m : TicketWinnings.GetWinningAmount(x, draw);
                    return new TicketForDrawDto(
                        x.Id,
                        x.DrawId,
                        x.Numbers,
                        DrawManagement.ToTicketStatusValue(x.Status),
                        x.PurchasedAtUtc,
                        winningAmount);
                })
                .ToList();

            var currentTickets = currentDraw is null
                ? Array.Empty<TicketForDrawDto>()
                : tickets
                    .Where(x => x.DrawId == currentDraw.Id)
                    .OrderByDescending(x => x.PurchasedAtUtc)
                    .ToArray();

            var activeDrawIds = activeDraws.Select(x => x.Id).ToHashSet();

            var activeTicketGroups = activeDraws
                .Select(draw => new DrawGroupDto(
                    draw.Id,
                    draw,
                    tickets
                        .Where(x => x.DrawId == draw.Id)
                        .OrderByDescending(x => x.PurchasedAtUtc)
                        .ToArray()))
                .ToArray();

            var historyTicketGroups = tickets
                .Where(x => !activeDrawIds.Contains(x.DrawId))
                .GroupBy(x => x.DrawId)
                .OrderByDescending(g => g.Key)
                .ToList();

            var historyDrawIds = historyTicketGroups.Select(x => x.Key).Distinct().ToArray();
            var historyDraws = historyDrawIds.Length == 0
                ? new Dictionary<long, DrawDto>()
                : (await db.Draws
                    .Where(x => historyDrawIds.Contains(x.Id))
                    .AsNoTracking()
                    .ToListAsync(ct))
                    .Select(DrawManagement.ToDto)
                    .ToDictionary(x => x.Id);

            var history = historyTicketGroups
                .Select(g =>
                {
                    historyDraws.TryGetValue(g.Key, out var draw);
                    return new DrawGroupDto(g.Key, draw, g.OrderByDescending(x => x.PurchasedAtUtc).ToArray());
                })
                .ToArray();

            var purchaseSettings = await ticketPurchaseSettings.GetSettingsAsync(ct);
            var ticketPurchase = new TicketPurchaseConfigDto(
                purchaseSettings.TicketSlotsCount,
                DrawManagement.NumbersPerDraw,
                DrawManagement.MinNumber,
                DrawManagement.MaxNumber);

            var state = new MiniAppStateDto(user?.Balance ?? 0m, currentDraw, activeDraws, activeTicketGroups, currentTickets, history, ticketPurchase);
            return Results.Ok(new { ok = true, state });
        });

        return endpoints;
    }
}


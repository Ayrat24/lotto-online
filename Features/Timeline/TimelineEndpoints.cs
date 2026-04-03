using Microsoft.EntityFrameworkCore;
using MiniApp.Data;
using MiniApp.Features.Auth;
using MiniApp.Features.Draws;
using MiniApp.Features.Tickets;
using MiniApp.TelegramLogin;

namespace MiniApp.Features.Timeline;

public static class TimelineEndpoints
{
    public static IEndpointRouteBuilder MapTimelineEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Returns the current draw, the user's tickets for it, and grouped personal history for previous draws.
        endpoints.MapPost("/api/timeline", async (
            PurchaseTicketRequest req,
            HttpContext http,
            IConfiguration config,
            IWebHostEnvironment env,
            AppDbContext db,
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

            var currentDrawEntity = await db.Draws
                .Where(x => x.State == DrawState.Active)
                .OrderByDescending(x => x.Id)
                .AsNoTracking()
                .FirstOrDefaultAsync(ct);

            var currentDraw = currentDrawEntity is null
                ? null
                : DrawManagement.ToDto(currentDrawEntity);

            var tickets = user is null
                ? new List<TicketForDrawDto>()
                : await db.Tickets
                    .Where(x => x.UserId == user.Id)
                    .OrderByDescending(x => x.PurchasedAtUtc)
                    .Select(x => new TicketForDrawDto(x.Id, x.DrawId, x.Numbers, x.PurchasedAtUtc))
                    .AsNoTracking()
                    .ToListAsync(ct);

            var currentTickets = currentDraw is null
                ? Array.Empty<TicketForDrawDto>()
                : tickets
                    .Where(x => x.DrawId == currentDraw.Id)
                    .OrderByDescending(x => x.PurchasedAtUtc)
                    .ToArray();

            var historyTicketGroups = tickets
                .Where(x => currentDraw is null || x.DrawId != currentDraw.Id)
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

            var state = new MiniAppStateDto(currentDraw, currentTickets, history);
            return Results.Ok(new { ok = true, state });
        });

        return endpoints;
    }
}


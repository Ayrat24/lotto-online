using Microsoft.EntityFrameworkCore;
using MiniApp.Data;
using MiniApp.Features.Draws;
using MiniApp.TelegramLogin;

namespace MiniApp.Features.Timeline;

public static class TimelineEndpoints
{
    public static IEndpointRouteBuilder MapTimelineEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Returns UI model: groups grouped by drawId, containing optional draw (when already created) and user's tickets for that draw.
        endpoints.MapPost("/api/timeline", async (
            MiniApp.Features.Tickets.PurchaseTicketRequest req,
            HttpContext http,
            IConfiguration config,
            IWebHostEnvironment env,
            AppDbContext db,
            CancellationToken ct) =>
        {
            long telegramUserId;

            // Dev shortcut used by the JS when not running inside Telegram.
            if (env.IsDevelopment() && http.Request.Headers.TryGetValue("X-Dev-TelegramUserId", out var devTgUserIdStr)
                && long.TryParse(devTgUserIdStr.ToString(), out var devTgUserId)
                && devTgUserId > 0)
            {
                telegramUserId = devTgUserId;
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

            // Load user's tickets (if any)
            var tickets = user is null
                ? new List<TicketForDrawDto>()
                : await db.Tickets
                    .Where(x => x.UserId == user.Id)
                    .OrderByDescending(x => x.PurchasedAtUtc)
                    .Select(x => new TicketForDrawDto(x.Id, x.DrawId, x.Numbers, x.PurchasedAtUtc))
                    .AsNoTracking()
                    .ToListAsync(ct);

            var ticketsByDraw = tickets
                .GroupBy(t => t.DrawId)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<TicketForDrawDto>)g.OrderByDescending(x => x.PurchasedAtUtc).ToList());

            // Global draws (recent)
            var draws = await db.Draws
                .OrderByDescending(d => d.Id)
                .Take(100)
                .Select(d => new DrawDto(d.Id, d.Numbers, d.CreatedAtUtc))
                .AsNoTracking()
                .ToListAsync(ct);

            var drawsById = draws.ToDictionary(d => d.Id, d => d);

            // Next draw id is always last draw + 1.
            var lastDrawId = draws.Count == 0 ? 0 : draws.Max(x => x.Id);
            var nextDrawId = lastDrawId + 1;

            // Group ids shown in UI:
            // - all draws that exist
            // - all drawIds that user has tickets for (including upcoming ones)
            // - always include "next" draw so user sees the upcoming container even with no tickets
            var groupIds = new HashSet<long>();
            foreach (var d in draws)
                groupIds.Add(d.Id);
            foreach (var drawId in ticketsByDraw.Keys)
                groupIds.Add(drawId);
            groupIds.Add(nextDrawId);

            var ordered = groupIds.OrderByDescending(x => x).ToArray();
            var groups = new List<DrawGroupDto>(ordered.Length);
            foreach (var drawId in ordered)
            {
                drawsById.TryGetValue(drawId, out var draw);
                ticketsByDraw.TryGetValue(drawId, out var groupTickets);
                groupTickets ??= Array.Empty<TicketForDrawDto>();

                groups.Add(new DrawGroupDto(drawId, draw, groupTickets));
            }

            return Results.Ok(new { ok = true, groups, nextDrawId });
        });

        return endpoints;
    }
}


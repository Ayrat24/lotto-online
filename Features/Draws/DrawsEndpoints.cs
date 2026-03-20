using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MiniApp.Admin;
using MiniApp.Data;
using MiniApp.TelegramLogin;

namespace MiniApp.Features.Draws;

public static class DrawsEndpoints
{
    public static IEndpointRouteBuilder MapDrawsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Admin: start a new draw (generate 6 numbers).
        endpoints.MapPost("/api/admin/draws/start", [Authorize(Policy = AdminAuth.PolicyName)] async (
            AppDbContext db,
            CancellationToken ct) =>
        {
            var draw = new Draw
            {
                Numbers = GenerateDrawNumbers(),
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            db.Draws.Add(draw);
            await db.SaveChangesAsync(ct);

            return Results.Ok(new { ok = true, draw = new DrawDto(draw.Id, draw.Numbers, draw.CreatedAtUtc) });
        });

        // Mini app: timeline (draw => tickets bought for that draw) grouped per draw.
        endpoints.MapPost("/api/timeline", async (
            MiniApp.Features.Tickets.PurchaseTicketRequest req,
            HttpContext http,
            IConfiguration config,
            IWebHostEnvironment env,
            AppDbContext db,
            CancellationToken ct) =>
        {
            long? telegramUserId = null;

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

            var u = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.TelegramUserId == telegramUserId!.Value, ct);
            if (u is null)
                return Results.Ok(new { ok = true, items = Array.Empty<TimelineItemDto>() });

            // Pull recent draws and all user's tickets for those draws.
            var draws = await db.Draws
                .OrderByDescending(x => x.CreatedAtUtc)
                .Take(50)
                .Select(x => new DrawDto(x.Id, x.Numbers, x.CreatedAtUtc))
                .AsNoTracking()
                .ToListAsync(ct);

            if (draws.Count == 0)
                return Results.Ok(new { ok = true, items = Array.Empty<TimelineItemDto>() });

            var drawIds = draws.Select(d => d.Id).ToArray();

            var tickets = await db.Tickets
                .Where(x => x.UserId == u.Id && drawIds.Contains(x.DrawId))
                .OrderByDescending(x => x.PurchasedAtUtc)
                .Select(x => new TicketForDrawDto(x.Id, x.DrawId, x.Numbers, x.PurchasedAtUtc))
                .AsNoTracking()
                .ToListAsync(ct);

            // Build timeline: for each draw (newest->oldest): draw, then tickets for that draw (newest->oldest)
            var ticketsByDraw = tickets
                .GroupBy(t => t.DrawId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.PurchasedAtUtc).ToList());

            var items = new List<TimelineItemDto>(draws.Count * 2);
            foreach (var d in draws)
            {
                items.Add(new TimelineItemDto("draw", d, null));
                if (ticketsByDraw.TryGetValue(d.Id, out var ts))
                {
                    foreach (var t in ts)
                        items.Add(new TimelineItemDto("ticket", null, t));
                }
            }

            return Results.Ok(new { ok = true, items });
        });

        return endpoints;
    }

    internal static string GenerateDrawNumbers()
    {
        // 6 distinct ints in [1..49], sorted.
        var set = new HashSet<int>();
        while (set.Count < 6)
            set.Add(Random.Shared.Next(1, 50));

        var arr = set.ToArray();
        Array.Sort(arr);
        return string.Join(',', arr);
    }
}

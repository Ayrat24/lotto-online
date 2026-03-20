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
        // Admin: start the next draw (generate 6 numbers) with the next sequential id.
        endpoints.MapPost("/api/admin/draws/start", [Authorize(Policy = AdminAuth.PolicyName)] async (
            AppDbContext db,
            CancellationToken ct) =>
        {
            var nextId = (await db.Draws.MaxAsync(x => (long?)x.Id, ct) ?? 0) + 1;

            var draw = new Draw
            {
                Id = nextId,
                Numbers = GenerateDrawNumbers(),
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            db.Draws.Add(draw);
            await db.SaveChangesAsync(ct);

            return Results.Ok(new { ok = true, draw = new DrawDto(draw.Id, draw.Numbers, draw.CreatedAtUtc) });
        });

        // Mini app: grouped feed: tickets are shown in containers assigned to a draw number.
        // When a draw happens, its result is shown as a prominent header above that container.
        endpoints.MapPost("/api/timeline", async (
            MiniApp.Features.Tickets.PurchaseTicketRequest req,
            HttpContext http,
            IConfiguration config,
            IWebHostEnvironment env,
            AppDbContext db,
            CancellationToken ct) =>
        {
            long? telegramUserId = null;

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
                return Results.Ok(new { ok = true, groups = Array.Empty<DrawGroupDto>(), nextDrawId = 1L });

            // Tickets grouped by their assigned drawId.
            var tickets = await db.Tickets
                .Where(x => x.UserId == u.Id)
                .OrderByDescending(x => x.PurchasedAtUtc)
                .Select(x => new TicketForDrawDto(x.Id, x.DrawId, x.Numbers, x.PurchasedAtUtc))
                .AsNoTracking()
                .ToListAsync(ct);

            var drawIds = tickets.Select(x => x.DrawId).Distinct().ToArray();

            // Fetch only draws that actually exist (admin-created) for those ids.
            var draws = await db.Draws
                .Where(d => drawIds.Contains(d.Id))
                .Select(d => new DrawDto(d.Id, d.Numbers, d.CreatedAtUtc))
                .AsNoTracking()
                .ToListAsync(ct);

            var drawsById = draws.ToDictionary(d => d.Id, d => d);

            var ticketsByDraw = tickets
                .GroupBy(t => t.DrawId)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<TicketForDrawDto>)g.OrderByDescending(x => x.PurchasedAtUtc).ToList());

            var groupIds = ticketsByDraw.Keys.OrderByDescending(x => x).ToArray();
            var groups = new List<DrawGroupDto>(groupIds.Length);

            foreach (var drawId in groupIds)
            {
                drawsById.TryGetValue(drawId, out var draw);
                groups.Add(new DrawGroupDto(drawId, draw, ticketsByDraw[drawId]));
            }

            var nextDrawId = (await db.Draws.MaxAsync(x => (long?)x.Id, ct) ?? 0) + 1;
            return Results.Ok(new { ok = true, groups, nextDrawId });
        });

        return endpoints;
    }

    internal static string GenerateDrawNumbers()
    {
        var set = new HashSet<int>();
        while (set.Count < 6)
            set.Add(Random.Shared.Next(1, 50));

        var arr = set.ToArray();
        Array.Sort(arr);
        return string.Join(',', arr);
    }
}

using Microsoft.EntityFrameworkCore;
using MiniApp.Data;
using MiniApp.TelegramLogin;

namespace MiniApp.Features.Tickets;

public static class TicketsEndpoints
{
    public static IEndpointRouteBuilder MapTicketsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // List tickets for current Telegram user.
        endpoints.MapPost("/api/tickets/list", async (
            PurchaseTicketRequest req,
            IConfiguration config,
            IWebHostEnvironment env,
            AppDbContext db,
            CancellationToken ct) =>
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

            var userId = tgUser!.Id;
            var u = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.TelegramUserId == userId, ct);
            if (u is null)
                return Results.Ok(new { ok = true, tickets = Array.Empty<TicketDto>() });

            var tickets = await db.Tickets
                .Where(x => x.UserId == u.Id)
                .OrderByDescending(x => x.PurchasedAtUtc)
                .Select(x => new TicketDto(x.Id, x.Numbers, x.PurchasedAtUtc))
                .AsNoTracking()
                .ToListAsync(ct);

            return Results.Ok(new { ok = true, tickets });
        });

        // Purchase a ticket (server-side random numbers).
        endpoints.MapPost("/api/tickets/purchase", async (
            PurchaseTicketRequest req,
            IConfiguration config,
            IWebHostEnvironment env,
            AppDbContext db,
            IUserService users,
            CancellationToken ct) =>
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

            var telegramUserId = tgUser!.Id;
            var u = await users.TouchUserAsync(telegramUserId, ct);

            var numbers = GenerateTicketNumbers();
            var ticket = new Ticket
            {
                UserId = u.Id,
                Numbers = numbers,
                PurchasedAtUtc = DateTimeOffset.UtcNow
            };

            db.Tickets.Add(ticket);
            await db.SaveChangesAsync(ct);

            return Results.Ok(new { ok = true, ticket = new TicketDto(ticket.Id, ticket.Numbers, ticket.PurchasedAtUtc) });
        });

        return endpoints;
    }

    private static string GenerateTicketNumbers()
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


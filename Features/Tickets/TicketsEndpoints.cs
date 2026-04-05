using Microsoft.EntityFrameworkCore;
using MiniApp.Data;
using MiniApp.Features.Auth;
using MiniApp.Features.Draws;
using MiniApp.TelegramLogin;

namespace MiniApp.Features.Tickets;

public static class TicketsEndpoints
{
    public static IEndpointRouteBuilder MapTicketsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // List tickets for current Telegram user.
        endpoints.MapPost("/api/tickets/list", async (
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

            var u = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.TelegramUserId == telegramUserId, ct);
            if (u is null)
                return Results.Ok(new { ok = true, tickets = Array.Empty<TicketDto>() });

            var tickets = await db.Tickets
                .Where(x => x.UserId == u.Id)
                .OrderByDescending(x => x.PurchasedAtUtc)
                .Select(x => new TicketDto(x.Id, x.DrawId, x.Numbers, x.PurchasedAtUtc))
                .AsNoTracking()
                .ToListAsync(ct);

            return Results.Ok(new { ok = true, tickets });
        });

        // Purchase a ticket for the current active draw using user-selected numbers.
        endpoints.MapPost("/api/tickets/purchase", async (
            PurchaseTicketRequest req,
            HttpContext http,
            IConfiguration config,
            IWebHostEnvironment env,
            AppDbContext db,
            IUserService users,
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

            var u = await users.TouchUserAsync(telegramUserId, ct);

            var currentDraw = await db.Draws
                .Where(x => x.State == DrawState.Active)
                .OrderByDescending(x => x.Id)
                .Select(x => new { x.Id })
                .FirstOrDefaultAsync(ct);

            if (currentDraw is null)
                return Results.BadRequest(new { ok = false, error = "There is no active draw right now." });

            if (!TryNormalizeSelectedNumbers(req.Numbers, out var numbers, out var validationError))
                return Results.BadRequest(new { ok = false, error = validationError });

            var ticket = new Ticket
            {
                UserId = u.Id,
                DrawId = currentDraw.Id,
                Numbers = numbers,
                PurchasedAtUtc = DateTimeOffset.UtcNow
            };

            db.Tickets.Add(ticket);
            await db.SaveChangesAsync(ct);

            return Results.Ok(new { ok = true, ticket = new TicketDto(ticket.Id, ticket.DrawId, ticket.Numbers, ticket.PurchasedAtUtc) });
        });

        return endpoints;
    }

    private static bool TryNormalizeSelectedNumbers(IReadOnlyList<int>? selectedNumbers, out string normalizedNumbers, out string error)
    {
        normalizedNumbers = string.Empty;
        error = string.Empty;

        if (selectedNumbers is null)
        {
            error = "Please select numbers first.";
            return false;
        }

        if (selectedNumbers.Count != DrawManagement.NumbersPerDraw)
        {
            error = $"Exactly {DrawManagement.NumbersPerDraw} numbers are required.";
            return false;
        }

        var set = new HashSet<int>();
        foreach (var n in selectedNumbers)
        {
            if (n < DrawManagement.MinNumber || n > DrawManagement.MaxNumber)
            {
                error = $"Each number must be between {DrawManagement.MinNumber} and {DrawManagement.MaxNumber}.";
                return false;
            }

            if (!set.Add(n))
            {
                error = "Numbers must be unique.";
                return false;
            }
        }

        var arr = set.ToArray();
        Array.Sort(arr);
        normalizedNumbers = string.Join(',', arr);
        return true;
    }
}

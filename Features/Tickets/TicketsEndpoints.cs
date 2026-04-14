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

            var ticketRows = await db.Tickets
                .Where(x => x.UserId == u.Id)
                .OrderByDescending(x => x.PurchasedAtUtc)
                .Select(x => new { x.Id, x.DrawId, x.Numbers, x.Status, x.PurchasedAtUtc })
                .AsNoTracking()
                .ToListAsync(ct);

            var drawIds = ticketRows.Select(x => x.DrawId).Distinct().ToArray();
            var drawsById = drawIds.Length == 0
                ? new Dictionary<long, Draw>()
                : await db.Draws
                    .Where(x => drawIds.Contains(x.Id))
                    .AsNoTracking()
                    .ToDictionaryAsync(x => x.Id, ct);

            var tickets = ticketRows
                .Select(x =>
                {
                    drawsById.TryGetValue(x.DrawId, out var draw);
                    var winningAmount = draw is null
                        ? 0m
                        : TicketWinnings.GetWinningAmount(
                            new Ticket { Numbers = x.Numbers, Status = x.Status },
                            draw);
                    return new TicketDto(x.Id, x.DrawId, x.Numbers, DrawManagement.ToTicketStatusValue(x.Status), x.PurchasedAtUtc, winningAmount);
                })
                .ToArray();

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
            IWalletService wallet,
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

            var activeDraws = await db.Draws
                .Where(x => x.State == DrawState.Active)
                .OrderByDescending(x => x.Id)
                .Select(x => new { x.Id })
                .ToListAsync(ct);

            if (activeDraws.Count == 0)
                return Results.BadRequest(new { ok = false, error = "There is no active draw right now." });

            long selectedDrawId;
            if (req.DrawId.HasValue)
            {
                var requestedDrawId = req.DrawId.Value;
                if (!activeDraws.Any(x => x.Id == requestedDrawId))
                    return Results.BadRequest(new { ok = false, error = "Selected draw is not active." });

                selectedDrawId = requestedDrawId;
            }
            else
            {
                selectedDrawId = activeDraws[0].Id;
            }

            if (!TryNormalizeSelectedNumbers(req.Numbers, out var numbers, out var validationError))
                return Results.BadRequest(new { ok = false, error = validationError });

            var purchaseResult = await wallet.TryPurchaseTicketAsync(u.Id, selectedDrawId, numbers, ct);
            if (!purchaseResult.Success || purchaseResult.Ticket is null)
                return Results.BadRequest(new { ok = false, error = purchaseResult.Error ?? "Purchase failed.", balance = purchaseResult.UserBalance });

            var ticket = purchaseResult.Ticket;
            return Results.Ok(new
            {
                ok = true,
                balance = purchaseResult.UserBalance,
                ticket = new TicketDto(ticket.Id, ticket.DrawId, ticket.Numbers, DrawManagement.ToTicketStatusValue(ticket.Status), ticket.PurchasedAtUtc, 0m)
            });
        });

        endpoints.MapPost("/api/tickets/claim", async (
            ClaimTicketRequest req,
            HttpContext http,
            IConfiguration config,
            IWebHostEnvironment env,
            AppDbContext db,
            IUserService users,
            IWalletService wallet,
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
            var claim = await wallet.ClaimTicketWinningsAsync(u.Id, req.TicketId, ct);
            if (!claim.Success)
                return Results.BadRequest(new { ok = false, error = claim.Error ?? "Claim failed.", balance = claim.UserBalance });

            return Results.Ok(new { ok = true, amount = claim.Amount, balance = claim.UserBalance });
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

        // Keep the original user-selected order for player-facing views.
        normalizedNumbers = string.Join(',', selectedNumbers);
        return true;
    }
}

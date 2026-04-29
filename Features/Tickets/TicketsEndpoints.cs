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
            InitDataRequest req,
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

        // Purchase multiple tickets for a selected active draw in one batch.
        endpoints.MapPost("/api/tickets/purchase", async (
            PurchaseTicketsRequest req,
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
            var nowUtc = DateTimeOffset.UtcNow;

            var activeDraws = await db.Draws
                .Where(x => x.State == DrawState.Active)
                .OrderByDescending(x => x.Id)
                .Select(x => new { x.Id, x.PurchaseClosesAtUtc })
                .ToListAsync(ct);

            activeDraws = activeDraws
                .Where(x => x.PurchaseClosesAtUtc > nowUtc)
                .ToList();

            if (activeDraws.Count == 0)
                return Results.BadRequest(new { ok = false, error = "There is no active draw right now." });

            var selectedDraw = req.DrawId <= 0
                ? null
                : activeDraws.FirstOrDefault(x => x.Id == req.DrawId);

            if (selectedDraw is null)
                return Results.BadRequest(new { ok = false, error = "Selected draw is not active." });

            if (selectedDraw.PurchaseClosesAtUtc <= nowUtc)
                return Results.BadRequest(new { ok = false, error = "Ticket sales for this draw are closed." });

            if (!TryNormalizeSelectedTickets(req.Tickets, out var normalizedTickets, out var validationError))
                return Results.BadRequest(new { ok = false, error = validationError });

            var purchaseResult = await wallet.TryPurchaseTicketsAsync(u.Id, req.DrawId, normalizedTickets, req.OfferId, ct);
            if (!purchaseResult.Success || purchaseResult.Tickets is null)
                return Results.BadRequest(new
                {
                    ok = false,
                    error = purchaseResult.Error ?? "Purchase failed.",
                    errorCode = GetPurchaseErrorCode(purchaseResult.Error),
                    balance = purchaseResult.UserBalance,
                    totalCost = purchaseResult.TotalCost
                });

            var tickets = purchaseResult.Tickets
                .Select(ticket => new TicketDto(ticket.Id, ticket.DrawId, ticket.Numbers, DrawManagement.ToTicketStatusValue(ticket.Status), ticket.PurchasedAtUtc, 0m))
                .ToArray();

            return Results.Ok(new
            {
                ok = true,
                balance = purchaseResult.UserBalance,
                totalCost = purchaseResult.TotalCost,
                purchasedCount = tickets.Length,
                tickets
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

    private static bool TryNormalizeSelectedTickets(IReadOnlyList<IReadOnlyList<int>>? selectedTickets, out IReadOnlyList<string> normalizedTickets, out string error)
    {
        normalizedTickets = Array.Empty<string>();
        error = string.Empty;

        if (selectedTickets is null || selectedTickets.Count == 0)
        {
            error = "Select at least one completed ticket first.";
            return false;
        }

        var normalized = new List<string>(selectedTickets.Count);
        foreach (var selectedNumbers in selectedTickets)
        {
            if (selectedNumbers.Count != DrawManagement.NumbersPerDraw)
            {
                error = $"Exactly {DrawManagement.NumbersPerDraw} numbers are required per ticket.";
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

            normalized.Add(string.Join(',', selectedNumbers));
        }

        normalizedTickets = normalized;
        return true;
    }

    private static string? GetPurchaseErrorCode(string? error)
        => string.Equals(error, "Insufficient balance.", StringComparison.Ordinal)
            ? "insufficient_balance"
            : null;
}

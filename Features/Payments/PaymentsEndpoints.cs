using MiniApp.Data;
using MiniApp.Features.Auth;
using MiniApp.TelegramLogin;

namespace MiniApp.Features.Payments;

public static class PaymentsEndpoints
{
    public static IEndpointRouteBuilder MapPaymentsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/payments/deposits/create", async (
            CreateCryptoDepositRequest req,
            HttpContext http,
            IConfiguration config,
            IWebHostEnvironment env,
            AppDbContext db,
            IUserService users,
            IPaymentsService payments,
            CancellationToken ct) =>
        {
            var authResult = await TryResolveTelegramUserIdAsync(req.InitData, http, config, env, db, ct);
            if (authResult.ErrorResult is not null)
                return authResult.ErrorResult;

            var telegramUserId = authResult.TelegramUserId!.Value;
            var user = await users.TouchUserAsync(telegramUserId, ct);

            var result = await payments.CreateCryptoDepositAsync(user.Id, req.Amount, req.Currency, ct);
            if (!result.Success)
                return Results.BadRequest(new { ok = false, error = result.Error ?? "Failed to create deposit." });

            return Results.Ok(new { ok = true, deposit = result.Deposit });
        });

        endpoints.MapPost("/api/payments/deposits/status", async (
            GetCryptoDepositStatusRequest req,
            HttpContext http,
            IConfiguration config,
            IWebHostEnvironment env,
            AppDbContext db,
            IUserService users,
            IPaymentsService payments,
            CancellationToken ct) =>
        {
            var authResult = await TryResolveTelegramUserIdAsync(req.InitData, http, config, env, db, ct);
            if (authResult.ErrorResult is not null)
                return authResult.ErrorResult;

            var telegramUserId = authResult.TelegramUserId!.Value;
            var user = await users.TouchUserAsync(telegramUserId, ct);

            var result = await payments.GetCryptoDepositStatusAsync(user.Id, req.DepositId, ct);
            if (!result.Success)
                return Results.NotFound(new { ok = false, error = result.Error ?? "Deposit was not found." });

            return Results.Ok(new { ok = true, deposit = result.Deposit });
        });

        endpoints.MapPost("/api/webhooks/btcpay", async (
            HttpRequest request,
            IPaymentsService payments,
            CancellationToken ct) =>
        {
            using var reader = new StreamReader(request.Body);
            var body = await reader.ReadToEndAsync(ct);

            var deliveryId = request.Headers.TryGetValue("BTCPay-Delivery", out var deliveryHeader)
                ? deliveryHeader.ToString()
                : null;
            var signature = request.Headers.TryGetValue("BTCPay-Sig", out var sigHeader)
                ? sigHeader.ToString()
                : null;

            var result = await payments.ProcessBtcPayWebhookAsync(body, deliveryId, signature, ct);
            if (!result.Success)
                return Results.BadRequest(new { ok = false, error = result.Error ?? "Invalid webhook." });

            return Results.Ok(new { ok = true, duplicate = result.Duplicate });
        });

        return endpoints;
    }

    private static async Task<(long? TelegramUserId, IResult? ErrorResult)> TryResolveTelegramUserIdAsync(
        string initData,
        HttpContext http,
        IConfiguration config,
        IWebHostEnvironment env,
        AppDbContext db,
        CancellationToken ct)
    {
        if (LocalDebugMode.TryGetDebugTelegramUserId(http, config, env, out var localDebugUserId))
        {
            await LocalDebugSeed.EnsureSeededAsync(db, localDebugUserId, ct);
            return (localDebugUserId, null);
        }

        var botToken = config["BotToken"];
        if (string.IsNullOrWhiteSpace(botToken))
            return (null, Results.Problem("BotToken is not configured.", statusCode: 500));

        if (!TelegramInitDataValidator.TryValidateInitData(initData, botToken, TimeSpan.FromMinutes(10), out var tgUser, out var error))
        {
            if (env.IsDevelopment())
                return (null, Results.Json(new { ok = false, error }, statusCode: StatusCodes.Status401Unauthorized));
            return (null, Results.Unauthorized());
        }

        return (tgUser!.Id, null);
    }
}


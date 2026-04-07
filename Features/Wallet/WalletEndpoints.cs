using MiniApp.Data;
using MiniApp.Features.Auth;
using MiniApp.TelegramLogin;

namespace MiniApp.Features.Wallet;

public static class WalletEndpoints
{
    public static IEndpointRouteBuilder MapWalletEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/wallet/topup", async (
            WalletTopUpRequest req,
            HttpContext http,
            IConfiguration config,
            IWebHostEnvironment env,
            AppDbContext db,
            IUserService users,
            IWalletService wallet,
            CancellationToken ct) =>
        {
            var authResult = await TryResolveTelegramUserIdAsync(req.InitData, http, config, env, db, ct);
            if (authResult.ErrorResult is not null)
                return authResult.ErrorResult;

            var telegramUserId = authResult.TelegramUserId!.Value;

            var user = await users.TouchUserAsync(telegramUserId, ct);
            var balance = await wallet.TopUpUserAsync(user.Id, ct);

            return Results.Ok(new { ok = true, balance, added = wallet.TopUpAmount });
        });

        endpoints.MapPost("/api/wallet/withdraw", async (
            WalletWithdrawRequest req,
            HttpContext http,
            IConfiguration config,
            IWebHostEnvironment env,
            AppDbContext db,
            IUserService users,
            IWalletService wallet,
            CancellationToken ct) =>
        {
            var authResult = await TryResolveTelegramUserIdAsync(req.InitData, http, config, env, db, ct);
            if (authResult.ErrorResult is not null)
                return authResult.ErrorResult;

            var telegramUserId = authResult.TelegramUserId!.Value;
            var user = await users.TouchUserAsync(telegramUserId, ct);

            var result = await wallet.CreateWithdrawalRequestAsync(user.Id, req.Amount, req.Number, ct);
            if (!result.Success)
                return Results.BadRequest(new { ok = false, error = result.Error ?? "Withdrawal request failed.", balance = result.UserBalance });

            return Results.Ok(new
            {
                ok = true,
                balance = result.UserBalance,
                requestId = result.Request!.Id,
                amount = result.Request.Amount
            });
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





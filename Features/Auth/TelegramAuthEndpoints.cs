using MiniApp.Data;
using MiniApp.TelegramLogin;

namespace MiniApp.Features.Auth;

public static class TelegramAuthEndpoints
{
    public static IEndpointRouteBuilder MapTelegramAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/auth/telegram", async (
            TelegramAuthRequest req,
            HttpContext http,
            IConfiguration config,
            IWebHostEnvironment env,
            ILoggerFactory loggerFactory,
            AppDbContext db,
            IUserService users,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("TelegramAuth");
            var initData = req.InitData?.Trim() ?? string.Empty;

            var usesExplicitLocalDebug =
                string.Equals(initData, "local-debug", StringComparison.OrdinalIgnoreCase)
                || http.Request.Headers.ContainsKey("X-Dev-TelegramUserId");

            if (usesExplicitLocalDebug
                && LocalDebugMode.TryGetDebugTelegramUserId(http, config, env, out var debugTelegramUserId))
            {
                await LocalDebugSeed.EnsureSeededAsync(db, debugTelegramUserId, ct);
                var debugUser = await users.TouchUserAsync(debugTelegramUserId, ct);
                return Results.Ok(new TelegramAuthResult(
                    Ok: true,
                    TelegramUserId: debugUser.TelegramUserId,
                    Balance: debugUser.Balance,
                    Username: "debug-user",
                    FirstName: "Debug",
                    LastName: "User",
                    Error: null));
            }

            var botToken = config["BotToken"];
            if (string.IsNullOrWhiteSpace(botToken))
                return Results.Problem("BotToken is not configured.", statusCode: 500);

            if (!TelegramInitDataValidator.TryValidateInitData(initData, botToken, TimeSpan.FromMinutes(10), out var tgUser, out var startParam, out var error))
            {
                logger.LogWarning("Telegram initData validation failed: {Error}", error);

                // In development, return the error so you can debug quickly.
                if (env.IsDevelopment())
                    return Results.Json(new TelegramAuthResult(false, null, null, null, null, null, error), statusCode: StatusCodes.Status401Unauthorized);

                return Results.Unauthorized();
            }

            var u = await users.TouchUserAsync(tgUser!.Id, ct, startParam);
            logger.LogInformation("Upserted user with TelegramUserId={TelegramUserId}", u.TelegramUserId);

            return Results.Ok(new TelegramAuthResult(
                Ok: true,
                TelegramUserId: u.TelegramUserId,
                Balance: u.Balance,
                Username: tgUser.Username,
                FirstName: tgUser.FirstName,
                LastName: tgUser.LastName,
                Error: null));
        });

        return endpoints;
    }
}

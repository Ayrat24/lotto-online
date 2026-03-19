using MiniApp.Data;
using MiniApp.TelegramLogin;

namespace MiniApp.Features.Auth;

public static class TelegramAuthEndpoints
{
    public static IEndpointRouteBuilder MapTelegramAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/auth/telegram", async (
            TelegramAuthRequest req,
            IConfiguration config,
            IWebHostEnvironment env,
            ILoggerFactory loggerFactory,
            IUserService users,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("TelegramAuth");

            var botToken = config["BotToken"];
            if (string.IsNullOrWhiteSpace(botToken))
                return Results.Problem("BotToken is not configured.", statusCode: 500);

            if (!TelegramInitDataValidator.TryValidateInitData(req.InitData, botToken, TimeSpan.FromMinutes(10), out var tgUser, out var error))
            {
                logger.LogWarning("Telegram initData validation failed: {Error}", error);

                // In development, return the error so you can debug quickly.
                if (env.IsDevelopment())
                    return Results.Json(new TelegramAuthResult(false, null, null, null, null, error), statusCode: StatusCodes.Status401Unauthorized);

                return Results.Unauthorized();
            }

            var u = await users.TouchUserAsync(tgUser!.Id, ct);
            logger.LogInformation("Upserted user with TelegramUserId={TelegramUserId}", u.TelegramUserId);

            return Results.Ok(new TelegramAuthResult(
                Ok: true,
                TelegramUserId: u.TelegramUserId,
                Username: tgUser.Username,
                FirstName: tgUser.First_Name,
                LastName: tgUser.Last_Name,
                Error: null));
        });

        return endpoints;
    }
}

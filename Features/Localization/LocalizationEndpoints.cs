using MiniApp.Admin;
using MiniApp.Data;
using MiniApp.Features.Auth;
using MiniApp.TelegramLogin;

namespace MiniApp.Features.Localization;

public static class LocalizationEndpoints
{
    public static IEndpointRouteBuilder MapLocalizationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/localization/bootstrap", async (
            LocalizationBootstrapRequest req,
            HttpContext http,
            IConfiguration config,
            IWebHostEnvironment env,
            IUserService users,
            ILocalizationService localization,
            AppDbContext db,
            CancellationToken ct) =>
        {
            await localization.EnsureDefaultsAsync(ct);

            long telegramUserId;
            string? telegramLanguageCode = null;

            if (LocalDebugMode.TryGetDebugTelegramUserId(http, config, env, out var debugTelegramUserId))
            {
                await LocalDebugSeed.EnsureSeededAsync(db, debugTelegramUserId, ct);
                telegramUserId = debugTelegramUserId;
            }
            else
            {
                var botToken = config["BotToken"];
                if (string.IsNullOrWhiteSpace(botToken))
                    return Results.Problem("BotToken is not configured.", statusCode: 500);

                if (!TelegramInitDataValidator.TryValidateInitData(req.InitData, botToken, TimeSpan.FromMinutes(10), out var tgUser, out _))
                    return Results.Json(new LocalizationBootstrapResult(false, "en", "0", new Dictionary<string, string>(), "Unauthorized"), statusCode: StatusCodes.Status401Unauthorized);

                telegramUserId = tgUser!.Id;
                telegramLanguageCode = tgUser.LanguageCode;
            }

            var user = await users.TouchUserAsync(telegramUserId, ct);
            var locale = localization.NormalizeLocale(user.PreferredLanguage ?? telegramLanguageCode ?? req.Locale);

            if (!string.Equals(user.PreferredLanguage, locale, StringComparison.Ordinal))
                await users.SetPreferredLanguageAsync(telegramUserId, locale, ct);

            var dict = await localization.GetDictionaryAsync(locale, ct);
            var version = await localization.GetVersionAsync(ct);

            return Results.Ok(new LocalizationBootstrapResult(true, locale, version, dict));
        });

        endpoints.MapGet("/api/admin/localization", async (ILocalizationService localization, CancellationToken ct) =>
            Results.Ok(await localization.GetAdminItemsAsync(ct)))
            .RequireAuthorization(AdminAuth.PolicyName);

        endpoints.MapPost("/api/admin/localization", async (LocalizationAdminUpdateRequest req, ILocalizationService localization, CancellationToken ct) =>
        {
            var result = await localization.UpsertAsync(req, ct);
            return result.Ok ? Results.Ok(result) : Results.BadRequest(result);
        }).RequireAuthorization(AdminAuth.PolicyName);

        return endpoints;
    }
}



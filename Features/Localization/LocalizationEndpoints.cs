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
            var initData = req.InitData.Trim();

            var usesExplicitLocalDebug =
                string.Equals(initData, "local-debug", StringComparison.OrdinalIgnoreCase)
                || http.Request.Headers.ContainsKey("X-Dev-TelegramUserId");

            if (usesExplicitLocalDebug
                && LocalDebugMode.TryGetDebugTelegramUserId(http, config, env, out var debugTelegramUserId))
            {
                await LocalDebugSeed.EnsureSeededAsync(db, debugTelegramUserId, ct);
                telegramUserId = debugTelegramUserId;
            }
            else
            {
                var botToken = config["BotToken"];
                if (string.IsNullOrWhiteSpace(botToken))
                    return Results.Problem("BotToken is not configured.", statusCode: 500);

                if (!TelegramInitDataValidator.TryValidateInitData(initData, botToken, TimeSpan.FromMinutes(10), out var tgUser, out _))
                {
                    var unauthorizedDebug = new LocalizationBootstrapDebugInfo(
                        UsesExplicitLocalDebug: usesExplicitLocalDebug,
                        RequestLocale: req.Locale,
                        TelegramLanguageCode: null,
                        PreferredLanguageBeforeSave: null,
                        ResolvedLocale: "en");

                    return Results.Json(
                        new LocalizationBootstrapResult(false, "en", "0", new Dictionary<string, string>(), "Unauthorized", unauthorizedDebug),
                        statusCode: StatusCodes.Status401Unauthorized);
                }

                telegramUserId = tgUser!.Id;
                telegramLanguageCode = tgUser.LanguageCode;
            }

            var user = await users.TouchUserAsync(telegramUserId, ct);
            var preferredLanguageBeforeSave = user.PreferredLanguage;
            var locale = localization.NormalizeLocale(preferredLanguageBeforeSave ?? telegramLanguageCode ?? req.Locale);

            if (!string.Equals(user.PreferredLanguage, locale, StringComparison.Ordinal))
                await users.SetPreferredLanguageAsync(telegramUserId, locale, ct);

            var dict = await localization.GetDictionaryAsync(locale, ct);
            var version = await localization.GetVersionAsync(ct);
            var debug = new LocalizationBootstrapDebugInfo(
                UsesExplicitLocalDebug: usesExplicitLocalDebug,
                RequestLocale: req.Locale,
                TelegramLanguageCode: telegramLanguageCode,
                PreferredLanguageBeforeSave: preferredLanguageBeforeSave,
                ResolvedLocale: locale);

            return Results.Ok(new LocalizationBootstrapResult(true, locale, version, dict, null, debug));
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



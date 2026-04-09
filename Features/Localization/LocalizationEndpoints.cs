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
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("LocalizationBootstrap");
            var stage = "init";
            var initData = req.InitData.Trim();
            var usesExplicitLocalDebug =
                string.Equals(initData, "local-debug", StringComparison.OrdinalIgnoreCase)
                || http.Request.Headers.ContainsKey("X-Dev-TelegramUserId");

            try
            {
                stage = "ensure-defaults";
                await localization.EnsureDefaultsAsync(ct);

                long telegramUserId;
                string? telegramLanguageCode = null;

                if (usesExplicitLocalDebug
                    && LocalDebugMode.TryGetDebugTelegramUserId(http, config, env, out var debugTelegramUserId))
                {
                    stage = "seed-local-debug";
                    await LocalDebugSeed.EnsureSeededAsync(db, debugTelegramUserId, ct);
                    telegramUserId = debugTelegramUserId;
                }
                else
                {
                    stage = "validate-bot-token";
                    var botToken = config["BotToken"];
                    if (string.IsNullOrWhiteSpace(botToken))
                    {
                        var tokenErrorDebug = new LocalizationBootstrapDebugInfo(
                            UsesExplicitLocalDebug: usesExplicitLocalDebug,
                            RequestLocale: req.Locale,
                            TelegramLanguageCode: null,
                            PreferredLanguageBeforeSave: null,
                            ResolvedLocale: "en",
                            FailureStage: stage,
                            TraceId: http.TraceIdentifier,
                            FailureDetails: "BotToken is not configured.");

                        return Results.Json(
                            new LocalizationBootstrapResult(false, "en", "0", new Dictionary<string, string>(), "BotToken is not configured.", tokenErrorDebug),
                            statusCode: StatusCodes.Status500InternalServerError);
                    }

                    stage = "validate-init-data";
                    if (!TelegramInitDataValidator.TryValidateInitData(initData, botToken, TimeSpan.FromMinutes(10), out var tgUser, out _))
                    {
                        var unauthorizedDebug = new LocalizationBootstrapDebugInfo(
                            UsesExplicitLocalDebug: usesExplicitLocalDebug,
                            RequestLocale: req.Locale,
                            TelegramLanguageCode: null,
                            PreferredLanguageBeforeSave: null,
                            ResolvedLocale: "en",
                            FailureStage: stage,
                            TraceId: http.TraceIdentifier,
                            FailureDetails: "Unauthorized initData.");

                        return Results.Json(
                            new LocalizationBootstrapResult(false, "en", "0", new Dictionary<string, string>(), "Unauthorized", unauthorizedDebug),
                            statusCode: StatusCodes.Status401Unauthorized);
                    }

                    telegramUserId = tgUser!.Id;
                    telegramLanguageCode = tgUser.LanguageCode;
                }

                stage = "resolve-user";
                var user = await users.TouchUserAsync(telegramUserId, ct);
                var preferredLanguageBeforeSave = user.PreferredLanguage;
                var locale = localization.NormalizeLocale(preferredLanguageBeforeSave ?? telegramLanguageCode ?? req.Locale);

                stage = "persist-locale";
                if (!string.Equals(user.PreferredLanguage, locale, StringComparison.Ordinal))
                    await users.SetPreferredLanguageAsync(telegramUserId, locale, ct);

                stage = "load-dictionary";
                var dict = await localization.GetDictionaryAsync(locale, ct);
                var version = await localization.GetVersionAsync(ct);
                var debug = new LocalizationBootstrapDebugInfo(
                    UsesExplicitLocalDebug: usesExplicitLocalDebug,
                    RequestLocale: req.Locale,
                    TelegramLanguageCode: telegramLanguageCode,
                    PreferredLanguageBeforeSave: preferredLanguageBeforeSave,
                    ResolvedLocale: locale,
                    TraceId: http.TraceIdentifier);

                return Results.Ok(new LocalizationBootstrapResult(true, locale, version, dict, null, debug));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Localization bootstrap failed at stage {Stage}. TraceId={TraceId}", stage, http.TraceIdentifier);

                var publicError = env.IsDevelopment()
                    ? ex.Message
                    : $"Localization bootstrap failed at stage '{stage}'.";
                var details = env.IsDevelopment() ? ex.Message : null;
                var errorDebug = new LocalizationBootstrapDebugInfo(
                    UsesExplicitLocalDebug: usesExplicitLocalDebug,
                    RequestLocale: req.Locale,
                    TelegramLanguageCode: null,
                    PreferredLanguageBeforeSave: null,
                    ResolvedLocale: "en",
                    FailureStage: stage,
                    TraceId: http.TraceIdentifier,
                    FailureDetails: details);

                return Results.Json(
                    new LocalizationBootstrapResult(false, "en", "0", new Dictionary<string, string>(), publicError, errorDebug),
                    statusCode: StatusCodes.Status500InternalServerError);
            }
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



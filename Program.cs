using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using MiniApp.Admin;
using MiniApp.Data;
using MiniApp.Features.Auth;
using MiniApp.Features.Draws;
using MiniApp.Features.Tickets;
using MiniApp.Features.Users;
using MiniApp.Features.Timeline;
using MiniApp.Features.Wallet;
using MiniApp.Features.Payments;
using MiniApp.Features.Localization;
using MiniApp.Features.NewsBanners;
using MiniApp.Features.Referrals;
using MiniApp.Features.Winners;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

ApplyLocalDotEnvConfiguration(builder);

// ===== Config =====
var localDebugRequested = builder.Configuration.GetValue<bool>("LocalDebug:Enabled");
if (localDebugRequested && !builder.Environment.IsDevelopment())
    throw new InvalidOperationException("LocalDebug is only allowed in Development.");

var localDebugEnabled = LocalDebugMode.IsEnabled(builder.Configuration, builder.Environment);
var telegramEnabled = !localDebugEnabled;

var token = builder.Configuration["BotToken"];
if (telegramEnabled && (string.IsNullOrWhiteSpace(token) || token == "YOUR_BOT_TOKEN"))
    throw new InvalidOperationException("Set BotToken in appsettings.json (or User Secrets) to your real Telegram token.");

var botMode = builder.Configuration["BotMode"]; // Polling | Webhook
if (string.IsNullOrWhiteSpace(botMode)) botMode = "Polling";

var webAppUrl = builder.Configuration["BotWebAppUrl"]; // public https base url for the mini app (domain or reverse proxy)
var miniAppText = builder.Configuration["MiniApp:Text"] ?? "(No MiniApp:Text configured)";

// ===== Services =====
builder.Services.AddRazorPages();
builder.Services.AddMemoryCache();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedProto
        | ForwardedHeaders.XForwardedHost;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Admin panel (cookie auth)
builder.Services.AddAdminArea(builder.Configuration);

// PostgreSQL + EF Core (modular)
builder.Services.AddPostgresDatabase(builder.Configuration);

builder.Services
    .AddOptions<PaymentsOptions>()
    .Bind(builder.Configuration.GetSection(PaymentsOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddSingleton<IValidateOptions<PaymentsOptions>, PaymentsOptionsValidator>();

builder.Services.AddHttpClient<IBtcPayClient, BtcPayClient>((sp, http) =>
{
    var options = sp.GetRequiredService<IOptions<PaymentsOptions>>().Value.BtcPay;
    var timeoutSeconds = options.RequestTimeoutSeconds <= 0 ? 15 : options.RequestTimeoutSeconds;
    http.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
});
builder.Services.AddHttpClient<ITelegramTonClient, TelegramTonClient>((sp, http) =>
{
    var options = sp.GetRequiredService<IOptions<PaymentsOptions>>().Value.TelegramTon;
    var timeoutSeconds = options.RequestTimeoutSeconds <= 0 ? 15 : options.RequestTimeoutSeconds;
    http.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
});
builder.Services.AddSingleton<ITelegramTonRateService, TelegramTonRateService>();
builder.Services.AddSingleton<ITelegramTonHotWalletService, TelegramTonHotWalletService>();
builder.Services.AddScoped<TelegramTonWithdrawalProcessor>();
builder.Services.AddHostedService<TelegramTonRateRefreshHostedService>();
builder.Services.AddHostedService<TelegramTonDepositReconciliationHostedService>();
builder.Services.AddHostedService<TelegramTonWithdrawalHostedService>();
builder.Services.AddScoped<IPaymentsService, PaymentsService>();
builder.Services.AddScoped<ILocalizationService, LocalizationService>();

if (telegramEnabled)
{
    builder.Services
        .AddHttpClient("tg")
        .RemoveAllLoggers()
        .AddTypedClient(httpClient => new TelegramBotClient(token!, httpClient));

    builder.Services.AddSingleton(new BotSettings
    {
        Mode = botMode,
        WebAppUrl = webAppUrl
    });

    // Polling runs in-process and doesn't need any inbound traffic.
    // In Webhook mode we don't start polling, otherwise Telegram will respond with 409 conflicts.
    if (string.Equals(botMode, "Polling", StringComparison.OrdinalIgnoreCase))
    {
        builder.Services.AddHostedService<TelegramPollingService>();
    }
}

var app = builder.Build();

// Make DI available to the bot update handler.
if (telegramEnabled)
{
    BotUpdateHandler.Services = app.Services;
}

// Auto-apply EF Core migrations on startup.
// Default: enabled in Development, disabled in other environments.
// Override via configuration: Database:AutoMigrate=true|false
// (e.g. env var: Database__AutoMigrate=true)
var autoMigrate = app.Configuration.GetValue<bool?>("Database:AutoMigrate")
                 ?? app.Environment.IsDevelopment();

if (autoMigrate)
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseMigrations");
    try
    {
        logger.LogInformation("Applying database migrations (AutoMigrate enabled)...");
        var migrationsApplied = await app.ApplyMigrationsAsync();
        if (migrationsApplied)
            logger.LogInformation("Database migrations applied.");
        else
            logger.LogInformation("Skipping database migrations for non-relational local debug provider.");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Failed to apply database migrations.");
        throw;
    }
}

// ===== Middleware =====
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();

var provider = new FileExtensionContentTypeProvider
{
    Mappings = { [".tgs"] = "application/x-tgsticker" }
};
app.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = provider });

app.UseRouting();

app.UseAuthentication();

if (localDebugEnabled)
{
    app.Use(async (http, next) =>
    {
        if (LocalDebugMode.IsLocalRequest(http)
            && !(http.User.Identity?.IsAuthenticated ?? false)
            && (http.Request.Path.StartsWithSegments("/Admin", StringComparison.OrdinalIgnoreCase)
                || http.Request.Path.StartsWithSegments("/api/admin", StringComparison.OrdinalIgnoreCase)))
        {
            var debugAdminUsername = LocalDebugMode.GetAdminUsername(app.Configuration);
            await AdminAuth.SignInAdminAsync(http, debugAdminUsername);
            http.User = AdminAuth.CreateAdminPrincipal(debugAdminUsername);
        }

        await next();
    });
}

app.UseAuthorization();

app.MapRazorPages();

app.MapPost("/Admin/set-language", async (HttpContext http, CancellationToken ct) =>
{
    var form = http.Request.HasFormContentType
        ? await http.Request.ReadFormAsync(ct)
        : null;

    var lang = form?["lang"].ToString();
    var returnUrl = form?["returnUrl"].ToString();

    var normalized = (lang ?? string.Empty).Trim().ToLowerInvariant();
    if (normalized != "ru" && normalized != "uz")
        normalized = "en";

    http.Response.Cookies.Append(
        "AdminUiLanguage",
        normalized,
        new CookieOptions
        {
            HttpOnly = false,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = http.Request.IsHttps,
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            Path = "/Admin"
        });

    if (!string.IsNullOrWhiteSpace(returnUrl)
        && returnUrl.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase)
        && !returnUrl.StartsWith("//", StringComparison.Ordinal))
    {
        return Results.LocalRedirect(returnUrl);
    }

    return Results.LocalRedirect("/Admin");
}).RequireAuthorization(AdminAuth.PolicyName);

// ===== Mini app backend APIs =====
app.MapGet("/api/text", () => Results.Ok(new { text = miniAppText }));
app.MapUsersEndpoints();
app.MapTelegramAuthEndpoints();
app.MapTicketsEndpoints();
app.MapDrawsEndpoints();
app.MapTimelineEndpoints();
app.MapWalletEndpoints();
app.MapPaymentsEndpoints();
app.MapLocalizationEndpoints();
app.MapNewsBannersEndpoints();
app.MapWinnersEndpoints();
app.MapReferralsEndpoints();

// Small health check / default landing page
app.MapGet("/", () => Results.Redirect(localDebugEnabled ? "/local-debug" : "/Admin"));

if (localDebugEnabled)
{
    app.MapGet("/local-debug", (HttpContext http) =>
    {
        if (!LocalDebugMode.IsLocalRequest(http))
            return Results.NotFound();

        const string html = """
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>MiniApp Local Debug</title>
  <style>
    body { font-family: Arial, sans-serif; padding: 24px; max-width: 720px; margin: 0 auto; }
    .row { display: flex; gap: 12px; flex-wrap: wrap; margin-top: 16px; }
    a, button { padding: 10px 14px; border: 1px solid #ccc; border-radius: 8px; text-decoration: none; background: #fff; cursor: pointer; }
  </style>
</head>
<body>
  <h1>Local Debug Mode</h1>
  <p>Opening mini app and admin pages for local development.</p>
  <div class="row">
    <a href="/app" target="_self">Open Mini App</a>
    <a href="/Admin" target="_blank" rel="noopener">Open Admin</a>
    <button id="openBoth" type="button">Open Both</button>
  </div>
  <script>
    (function () {
      var btn = document.getElementById('openBoth');
      if (!btn) return;
      btn.addEventListener('click', function () {
        try { window.open('/Admin', '_blank', 'noopener'); } catch (e) {}
        window.location.href = '/app';
      });
      btn.click();
    })();
  </script>
</body>
</html>
""";

        return Results.Content(html, "text/html");
    });
}

// ===== Telegram bot endpoints =====
static bool IsValidPublicHttpsBaseUrl(string? url)
    => !string.IsNullOrWhiteSpace(url)
       && url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
       && !url.Contains("localhost", StringComparison.OrdinalIgnoreCase)
       && !url.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase)
       && !url.Contains("[::1]", StringComparison.OrdinalIgnoreCase);

if (telegramEnabled)
{
    // Set the webhook (call this after setting BotWebAppUrl to your public https domain)
    app.MapGet("/bot/setWebhook", async (TelegramBotClient bot, BotSettings settings, CancellationToken ct) =>
    {
        if (!string.Equals(settings.Mode, "Webhook", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest("Set BotMode=Webhook first.");

        var baseUrl = PublicWebAppUrlResolver.ResolveBaseUrl(settings.WebAppUrl);
        if (!IsValidPublicHttpsBaseUrl(baseUrl))
            return Results.BadRequest("Set BotWebAppUrl to your public https:// domain first. Do not use localhost.");

        var webhookUrl = baseUrl!.TrimEnd('/') + "/bot";
        await bot.SetWebhook(webhookUrl, cancellationToken: ct);
        return Results.Text($"Webhook set to {webhookUrl}");
    });

    // Show current webhook info (useful for debugging)
    app.MapGet("/bot/webhookInfo", async (TelegramBotClient bot, CancellationToken ct) =>
    {
        var info = await bot.GetWebhookInfo(ct);
        return Results.Ok(info);
    });

    // Delete webhook (switching back to polling etc.)
    app.MapGet("/bot/deleteWebhook", async (TelegramBotClient bot, CancellationToken ct) =>
    {
        await bot.DeleteWebhook(dropPendingUpdates: false, cancellationToken: ct);
        return Results.Text("Webhook deleted");
    });

    // Webhook receiver endpoint (Telegram will POST updates here in Webhook mode)
    app.MapPost("/bot", async (
        [FromBody] Update update,
        TelegramBotClient bot,
        BotSettings settings,
        ILoggerFactory loggerFactory,
        CancellationToken ct) =>
    {
        var logger = loggerFactory.CreateLogger("TelegramWebhook");

        if (!string.Equals(settings.Mode, "Webhook", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Received /bot POST but BotMode is {Mode}. Ignoring.", settings.Mode);
            return Results.Ok();
        }

        try
        {
            await BotUpdateHandler.HandleUpdate(update, bot, settings, logger, ct);
            return Results.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to handle update");
            return Results.Ok();
        }
    });
}

app.Run();

static void ApplyLocalDotEnvConfiguration(WebApplicationBuilder builder)
{
    if (!builder.Environment.IsDevelopment())
        return;

    var dotEnvPath = Path.Combine(builder.Environment.ContentRootPath, ".env");
    if (!File.Exists(dotEnvPath))
        return;

    var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    foreach (var rawLine in File.ReadAllLines(dotEnvPath))
    {
        var line = rawLine.Trim();
        if (line.Length == 0 || line.StartsWith('#'))
            continue;

        var separatorIndex = line.IndexOf('=');
        if (separatorIndex <= 0)
            continue;

        var key = line[..separatorIndex].Trim();
        var value = line[(separatorIndex + 1)..].Trim();
        if (value.Length >= 2 && value.StartsWith('"') && value.EndsWith('"'))
            value = value[1..^1];

        AddMappedDotEnvValue(values, key, value);
    }

    if (values.Count > 0)
        builder.Configuration.AddInMemoryCollection(values);
}

static void AddMappedDotEnvValue(IDictionary<string, string?> values, string key, string value)
{
    if (key.Contains("__", StringComparison.Ordinal))
    {
        values[key.Replace("__", ":", StringComparison.Ordinal)] = value;
        return;
    }

    if (TryMapDockerStyleDotEnvKey(key, out var mappedKey))
    {
        values[mappedKey] = value;
        return;
    }

    values[key] = value;
}

static bool TryMapDockerStyleDotEnvKey(string key, out string mappedKey)
{
    mappedKey = key switch
    {
        "PAYMENTS_ENABLED" => "Payments:Enabled",
        "PAYMENTS_DEFAULT_PAYMENT_METHOD" => "Payments:DefaultPaymentMethod",
        "PAYMENTS_ENABLE_RECONCILIATION" => "Payments:Ops:EnableReconciliation",
        "BTCPAY_ENABLED" => "Payments:BtcPay:Enabled",
        "BTCPAY_BASE_URL" => "Payments:BtcPay:BaseUrl",
        "BTCPAY_STORE_ID" => "Payments:BtcPay:StoreId",
        "BTCPAY_API_KEY" => "Payments:BtcPay:ApiKey",
        "BTCPAY_WEBHOOK_SECRET" => "Payments:BtcPay:WebhookSecret",
        "BTCPAY_DEFAULT_CURRENCY" => "Payments:BtcPay:DefaultCurrency",
        "BTCPAY_REQUEST_TIMEOUT_SECONDS" => "Payments:BtcPay:RequestTimeoutSeconds",
        "BTCPAY_MAX_RETRY_ATTEMPTS" => "Payments:BtcPay:MaxRetryAttempts",
        "BTCPAY_WITHDRAWALS_PULL_PAYMENT_ID" => "Payments:BtcPay:WithdrawalsPullPaymentId",
        "BTCPAY_WITHDRAWALS_PAYMENT_METHOD" => "Payments:BtcPay:WithdrawalsPaymentMethod",
        "TELEGRAMTON_ENABLED" => "Payments:TelegramTon:Enabled",
        "TELEGRAMTON_TWA_RETURN_URL" => "Payments:TelegramTon:TwaReturnUrl",
        "TELEGRAMTON_MERCHANT_ADDRESS" => "Payments:TelegramTon:MerchantAddress",
        "TELEGRAMTON_MERCHANT_NAME" => "Payments:TelegramTon:MerchantName",
        "TELEGRAMTON_USD_PER_TON" => "Payments:TelegramTon:UsdPerTon",
        "TELEGRAMTON_AUTO_REFRESH_ENABLED" => "Payments:TelegramTon:AutoRefreshEnabled",
        "TELEGRAMTON_RATE_API_BASE_URL" => "Payments:TelegramTon:RateApiBaseUrl",
        "TELEGRAMTON_RATE_API_KEY" => "Payments:TelegramTon:RateApiKey",
        "TELEGRAMTON_RATE_REFRESH_INTERVAL_MINUTES" => "Payments:TelegramTon:RateRefreshIntervalMinutes",
        "TELEGRAMTON_MAX_RATE_AGE_MINUTES" => "Payments:TelegramTon:MaxRateAgeMinutes",
        "TELEGRAMTON_API_BASE_URL" => "Payments:TelegramTon:ApiBaseUrl",
        "TELEGRAMTON_API_KEY" => "Payments:TelegramTon:ApiKey",
        "TELEGRAMTON_REQUEST_TIMEOUT_SECONDS" => "Payments:TelegramTon:RequestTimeoutSeconds",
        "TELEGRAMTON_TRANSACTION_SEARCH_LIMIT" => "Payments:TelegramTon:TransactionSearchLimit",
        "TELEGRAMTON_DEPOSIT_MATCH_TOLERANCE_TON" => "Payments:TelegramTon:DepositMatchToleranceTon",
        "TELEGRAMTON_PAYMENT_TIMEOUT_MINUTES" => "Payments:TelegramTon:PaymentTimeoutMinutes",
        "TELEGRAMTON_EXPLORER_BASE_URL" => "Payments:TelegramTon:ExplorerBaseUrl",
        "TELEGRAMTON_SERVER_WITHDRAWALS_ENABLED" => "Payments:TelegramTon:ServerWithdrawalsEnabled",
        "TELEGRAMTON_RECONCILIATION_INTERVAL_SECONDS" => "Payments:TelegramTon:ReconciliationIntervalSeconds",
        "TELEGRAMTON_WITHDRAWAL_WORKER_INTERVAL_SECONDS" => "Payments:TelegramTon:WithdrawalWorkerIntervalSeconds",
        "TELEGRAMTON_WITHDRAWAL_CONFIRMATION_TIMEOUT_MINUTES" => "Payments:TelegramTon:WithdrawalConfirmationTimeoutMinutes",
        "TELEGRAMTON_WITHDRAWAL_MAX_RETRY_ATTEMPTS" => "Payments:TelegramTon:WithdrawalMaxRetryAttempts",
        "TELEGRAMTON_WITHDRAWAL_MESSAGE_TTL_SECONDS" => "Payments:TelegramTon:WithdrawalMessageTtlSeconds",
        "TELEGRAMTON_HOT_WALLET_WORKCHAIN" => "Payments:TelegramTon:HotWalletWorkchain",
        "TELEGRAMTON_HOT_WALLET_REVISION" => "Payments:TelegramTon:HotWalletRevision",
        "TELEGRAMTON_HOT_WALLET_SUBWALLET_ID" => "Payments:TelegramTon:HotWalletSubwalletId",
        "TELEGRAMTON_HOT_WALLET_MIN_RESERVE_TON" => "Payments:TelegramTon:HotWalletMinReserveTon",
        "TELEGRAMTON_HOT_WALLET_EXPECTED_ADDRESS" => "Payments:TelegramTon:HotWalletExpectedAddress",
        "TELEGRAMTON_HOT_WALLET_MNEMONIC" => "Payments:TelegramTon:HotWalletMnemonic",
        _ => string.Empty
    };

    return mappedKey.Length > 0;
}

// ===== Implementation =====

sealed class BotSettings
{
    public string Mode { get; init; } = "Polling";
    public string? WebAppUrl { get; init; }
}

static class PublicWebAppUrlResolver
{
    public static string? ResolveMiniAppUrl(string? configuredUrl)
    {
        var normalized = NormalizeAbsoluteUrl(configuredUrl);
        if (string.IsNullOrWhiteSpace(normalized))
            return string.IsNullOrWhiteSpace(configuredUrl)
                ? null
                : configuredUrl.TrimEnd('/') + "/app";

        return PathEndsWithSegment(normalized, "app")
            ? normalized
            : normalized.TrimEnd('/') + "/app";
    }

    public static string? ResolveBaseUrl(string? configuredUrl)
    {
        var normalized = NormalizeAbsoluteUrl(configuredUrl);
        if (string.IsNullOrWhiteSpace(normalized))
            return configuredUrl?.TrimEnd('/');

        return PathEndsWithSegment(normalized, "app")
            ? normalized[..^"/app".Length]
            : normalized;
    }

    private static string? NormalizeAbsoluteUrl(string? configuredUrl)
    {
        if (!Uri.TryCreate((configuredUrl ?? string.Empty).Trim(), UriKind.Absolute, out var uri))
            return null;

        var builder = new UriBuilder(uri)
        {
            Query = string.Empty,
            Fragment = string.Empty
        };

        return builder.Uri.ToString().TrimEnd('/');
    }

    private static bool PathEndsWithSegment(string absoluteUrl, string segment)
    {
        if (!Uri.TryCreate(absoluteUrl, UriKind.Absolute, out var uri))
            return false;

        var normalizedPath = (uri.AbsolutePath ?? string.Empty).TrimEnd('/');
        return normalizedPath.EndsWith("/" + segment.Trim('/'), StringComparison.OrdinalIgnoreCase);
    }
}

static class BotUpdateHandler
{
    public static IServiceProvider Services { get; set; } = default!;

    // Simple in-memory onboarding state: which telegram users are expected to send their number next.
    // Note: in Webhook mode with multiple instances this should be moved to durable storage.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<long, byte> AwaitingNumber = new();
    private static readonly string[] SupportedLanguages = ["en", "ru", "uz"];

    public static async Task HandleUpdate(Update update, TelegramBotClient bot, BotSettings settings, ILogger logger, CancellationToken ct)
    {
        // Resolve services from DI.
        await using var scope = Services.CreateAsyncScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var localization = scope.ServiceProvider.GetRequiredService<ILocalizationService>();

        static string NormalizeLanguageCode(string? languageCode)
        {
            var value = (languageCode ?? string.Empty).Trim().ToLowerInvariant();
            if (value.StartsWith("ru", StringComparison.Ordinal)) return "ru";
            if (value.StartsWith("uz", StringComparison.Ordinal)) return "uz";
            return "en";
        }

        static bool TryParseStartCommand(string? commandText, out string? payload)
        {
            payload = null;
            if (string.IsNullOrWhiteSpace(commandText))
                return false;

            var trimmed = commandText.Trim();
            if (!trimmed.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
                return false;

            var separatorIndex = trimmed.IndexOfAny([' ', '\t', '\r', '\n']);
            var commandToken = separatorIndex < 0 ? trimmed : trimmed[..separatorIndex];
            if (commandToken.Length > 6 && commandToken[6] != '@')
                return false;

            if (separatorIndex < 0)
                return true;

            var rawPayload = trimmed[(separatorIndex + 1)..].Trim();
            if (rawPayload.Length == 0)
                return true;

            payload = rawPayload.Length > 128 ? rawPayload[..128] : rawPayload;
            return true;
        }

        async Task<string> GetTextAsync(string locale, string key, string fallback)
            => await localization.GetTextAsync(locale, key, fallback, ct);

        static string? TryNormalizeNumber(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;

            // Keep digits and an optional leading '+'.
            input = input.Trim();
            var chars = input.Where(c => char.IsDigit(c) || c == '+').ToArray();
            if (chars.Length == 0) return null;

            var normalized = new string(chars);
            if (normalized.Count(c => c == '+') > 1) return null;
            if (normalized.Contains('+') && normalized[0] != '+') return null;

            // Basic sanity bounds.
            var digits = normalized.Count(char.IsDigit);
            if (digits < 6 || digits > 20) return null;

            return normalized;
        }

        async Task SendOpenWebAppButtonAsync(ChatId chatId, string locale)
        {
            var openLabel = await GetTextAsync(locale, "bot.openMiniApp", "Open Mini App");
            var changeLabel = await GetTextAsync(locale, "bot.changeLanguage", "Change language");

            var rows = new List<InlineKeyboardButton[]>();
            var miniAppUrl = PublicWebAppUrlResolver.ResolveMiniAppUrl(settings.WebAppUrl);
            if (!string.IsNullOrWhiteSpace(miniAppUrl))
            {
                rows.Add([
                    InlineKeyboardButton.WithWebApp(
                        openLabel,
                        miniAppUrl)
                ]);
            }

            rows.Add([
                InlineKeyboardButton.WithCallbackData(changeLabel, "lang:change")
            ]);

            var text = await GetTextAsync(locale, "bot.tapOpenMiniApp", "Tap the button below to open the Mini App.");

            await bot.SendMessage(
                chatId: chatId,
                text: text,
                replyMarkup: new InlineKeyboardMarkup(rows),
                cancellationToken: ct);
        }

        async Task SendLanguagePickerAsync(ChatId chatId, string locale)
        {
            var askLanguage = await GetTextAsync(locale, "bot.askLanguage", "Please choose your language:");

            await bot.SendMessage(
                chatId: chatId,
                text: askLanguage,
                replyMarkup: new InlineKeyboardMarkup([
                    [InlineKeyboardButton.WithCallbackData("English", "lang:set:en")],
                    [InlineKeyboardButton.WithCallbackData("Русский", "lang:set:ru")],
                    [InlineKeyboardButton.WithCallbackData("O'zbekcha", "lang:set:uz")]
                ]),
                cancellationToken: ct);
        }

        async Task AskForContactAsync(ChatId chatId, string locale)
        {
            var shareContact = await GetTextAsync(locale, "bot.shareContact", "Share my contact");
            var placeholder = await GetTextAsync(locale, "bot.askPhonePlaceholder", "Tap the button to share your phone number");
            var askContact = await GetTextAsync(locale, "bot.askContact", "Please share your contact to continue.");

            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new[] { KeyboardButton.WithRequestContact(shareContact) }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = true,
                InputFieldPlaceholder = placeholder
            };

            await bot.SendMessage(
                chatId: chatId,
                text: askContact,
                replyMarkup: keyboard,
                cancellationToken: ct);
        }

        if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery is not null)
        {
            var callback = update.CallbackQuery;
            var callbackUserId = callback.From.Id;
            var callbackChatId = new ChatId(callback.Message?.Chat.Id ?? callback.From.Id);
            var user = await userService.TouchUserAsync(callbackUserId, ct);
            var currentLocale = NormalizeLanguageCode(user.PreferredLanguage);
            var data = callback.Data?.Trim() ?? string.Empty;

            if (string.Equals(data, "lang:change", StringComparison.Ordinal))
            {
                await SendLanguagePickerAsync(callbackChatId, currentLocale);
                await bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
                return;
            }

            if (data.StartsWith("lang:set:", StringComparison.Ordinal))
            {
                var selected = NormalizeLanguageCode(data["lang:set:".Length..]);
                if (!SupportedLanguages.Contains(selected, StringComparer.Ordinal))
                    selected = "en";

                user = await userService.SetPreferredLanguageAsync(callbackUserId, selected, ct);

                var updated = await GetTextAsync(selected, "bot.languageUpdated", "Language updated.");
                await bot.AnswerCallbackQuery(callback.Id, updated, cancellationToken: ct);
                await bot.SendMessage(callbackChatId, updated, cancellationToken: ct);

                if (string.IsNullOrWhiteSpace(user.Number))
                {
                    AwaitingNumber[callbackUserId] = 0;
                    await AskForContactAsync(callbackChatId, selected);
                    return;
                }

                await SendOpenWebAppButtonAsync(callbackChatId, selected);
                return;
            }

            logger.LogInformation("Unhandled callback data: {Data}", data);
            return;
        }

        if (update.Type != UpdateType.Message || update.Message is null)
        {
            logger.LogInformation("Unhandled update type: {UpdateType}", update.Type);
            return;
        }

        var telegramUserId = update.Message.From?.Id;
        if (telegramUserId is null)
        {
            logger.LogWarning("Message without From.Id");
            return;
        }

        var text = update.Message.Text?.Trim();
        var isStartCommand = TryParseStartCommand(text, out var startPayload);
        var touchedUser = await userService.TouchUserAsync(telegramUserId.Value, ct, startPayload);
        var locale = NormalizeLanguageCode(touchedUser.PreferredLanguage);

        if (string.Equals(text, "/language", StringComparison.OrdinalIgnoreCase))
        {
            await SendLanguagePickerAsync(update.Message.Chat, locale);
            return;
        }

        // /start: ensure user exists, and if number isn't stored -> ask for contact.
        if (isStartCommand)
        {
            var user = touchedUser;
            if (string.IsNullOrWhiteSpace(user.PreferredLanguage))
            {
                await SendLanguagePickerAsync(update.Message.Chat, locale);
                return;
            }

            locale = NormalizeLanguageCode(user.PreferredLanguage);

            if (string.IsNullOrWhiteSpace(user.Number))
            {
                AwaitingNumber[telegramUserId.Value] = 0;
                await AskForContactAsync(update.Message.Chat, locale);
                return;
            }

            var welcome = await GetTextAsync(locale, "bot.welcomeBack", "Welcome back!");

            await bot.SendMessage(
                chatId: update.Message.Chat,
                text: welcome,
                cancellationToken: ct);

            await SendOpenWebAppButtonAsync(update.Message.Chat, locale);
            return;
        }

        // If we're awaiting a number/contact, accept Contact (preferred) or typed text.
        if (AwaitingNumber.TryRemove(telegramUserId.Value, out _))
        {
            if (string.IsNullOrWhiteSpace(touchedUser.PreferredLanguage))
            {
                AwaitingNumber[telegramUserId.Value] = 0;
                var askLanguageFirst = await GetTextAsync(locale, "bot.languageBeforePhone", "First choose a language.");
                await bot.SendMessage(update.Message.Chat, askLanguageFirst, cancellationToken: ct);
                await SendLanguagePickerAsync(update.Message.Chat, locale);
                return;
            }

            locale = NormalizeLanguageCode(touchedUser.PreferredLanguage);
            string? candidate = null;

            // Prefer contact share.
            if (update.Message.Contact?.PhoneNumber is { Length: > 0 } phone)
            {
                // Telegram may send without '+', normalize will handle.
                candidate = phone;
            }
            else
            {
                candidate = text;
            }

            var normalized = TryNormalizeNumber(candidate);
            if (normalized is null)
            {
                // Keep waiting.
                AwaitingNumber[telegramUserId.Value] = 0;

                var invalidNumber = await GetTextAsync(
                    locale,
                    "bot.invalidNumber",
                    "I couldn't read a valid phone number. Please tap 'Share my contact' or send the number as text (digits, optional leading +)." );

                await bot.SendMessage(
                    chatId: update.Message.Chat,
                    text: invalidNumber,
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: ct);

                await AskForContactAsync(update.Message.Chat, locale);
                return;
            }

            await userService.SetNumberAsync(telegramUserId.Value, normalized, ct);

            var saved = await GetTextAsync(locale, "bot.savedNumber", "Thanks! Saved.");

            await bot.SendMessage(
                chatId: update.Message.Chat,
                text: saved,
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: ct);

            await SendOpenWebAppButtonAsync(update.Message.Chat, locale);
            return;
        }

        logger.LogInformation("Message received: {Text}", text);
    }
}

sealed class TelegramPollingService : BackgroundService
{
    private readonly TelegramBotClient _bot;
    private readonly BotSettings _settings;
    private readonly ILogger<TelegramPollingService> _logger;

    public TelegramPollingService(TelegramBotClient bot, BotSettings settings, ILogger<TelegramPollingService> logger)
    {
        _bot = bot;
        _settings = settings;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!string.Equals(_settings.Mode, "Polling", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Polling disabled (BotMode={Mode}).", _settings.Mode);
            return;
        }

        // In polling mode, remove webhook so Telegram sends updates via getUpdates instead.
        try
        {
            await _bot.DeleteWebhook(dropPendingUpdates: false, cancellationToken: stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DeleteWebhook failed (often harmless). Возможно webhook не был установлен.");
        }

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        _logger.LogInformation("Starting Telegram polling...");

        _bot.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
        => BotUpdateHandler.HandleUpdate(update, (TelegramBotClient)bot, _settings, _logger, ct);

    private Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken ct)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiEx => $"Telegram API Error [{apiEx.ErrorCode}]: {apiEx.Message}",
            _ => exception.ToString()
        };

        _logger.LogError("Polling error: {Error}", errorMessage);
        return Task.CompletedTask;
    }
}

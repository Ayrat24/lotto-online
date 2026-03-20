using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using MiniApp.Admin;
using MiniApp.Data;
using MiniApp.Features.Auth;
using MiniApp.Features.Draws;
using MiniApp.Features.Tickets;
using MiniApp.Features.Users;
using MiniApp.Features.Timeline;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

var builder = WebApplication.CreateBuilder(args);

// ===== Config =====
var token = builder.Configuration["BotToken"];
if (string.IsNullOrWhiteSpace(token) || token == "YOUR_BOT_TOKEN")
    throw new InvalidOperationException("Set BotToken in appsettings.json (or User Secrets) to your real Telegram token.");

var botMode = builder.Configuration["BotMode"]; // Polling | Webhook
if (string.IsNullOrWhiteSpace(botMode)) botMode = "Polling";

var webAppUrl = builder.Configuration["BotWebAppUrl"]; // public https base url (ngrok/cloudflared)
var miniAppText = builder.Configuration["MiniApp:Text"] ?? "(No MiniApp:Text configured)";

// ===== Services =====
builder.Services.AddRazorPages();

// Admin panel (cookie auth)
builder.Services.AddAdminArea(builder.Configuration);

// PostgreSQL + EF Core (modular)
builder.Services.AddPostgresDatabase(builder.Configuration);

builder.Services
    .AddHttpClient("tg")
    .RemoveAllLoggers()
    .AddTypedClient(httpClient => new TelegramBotClient(token, httpClient));

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

var app = builder.Build();

// Make DI available to the bot update handler.
BotUpdateHandler.Services = app.Services;

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
        await app.ApplyMigrationsAsync();
        logger.LogInformation("Database migrations applied.");
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

app.UseHttpsRedirection();

var provider = new FileExtensionContentTypeProvider
{
    Mappings = { [".tgs"] = "application/x-tgsticker" }
};
app.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = provider });

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// ===== Mini app backend APIs =====
app.MapGet("/api/text", () => Results.Ok(new { text = miniAppText }));
app.MapUsersEndpoints();
app.MapTelegramAuthEndpoints();
app.MapTicketsEndpoints();
app.MapDrawsEndpoints();
app.MapTimelineEndpoints();

// Small health check / default landing page
app.MapGet("/", () => Results.Redirect("/Admin"));

// ===== Telegram bot endpoints =====
static bool IsValidPublicHttpsBaseUrl(string? url)
    => !string.IsNullOrWhiteSpace(url)
       && url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
       && !url.Contains("localhost", StringComparison.OrdinalIgnoreCase)
       && !url.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase)
       && !url.Contains("[::1]", StringComparison.OrdinalIgnoreCase);

// Set the webhook (call this after setting BotWebAppUrl to your ngrok/cloudflared https URL)
app.MapGet("/bot/setWebhook", async (TelegramBotClient bot, BotSettings settings, CancellationToken ct) =>
{
    if (!string.Equals(settings.Mode, "Webhook", StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest("Set BotMode=Webhook first.");

    var baseUrl = settings.WebAppUrl;
    if (!IsValidPublicHttpsBaseUrl(baseUrl))
        return Results.BadRequest("Set BotWebAppUrl to your public https:// URL first (ngrok/cloudflared). Do not use localhost.");

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

app.Run();

// ===== Implementation =====

sealed class BotSettings
{
    public string Mode { get; init; } = "Polling";
    public string? WebAppUrl { get; init; }
}

static class BotUpdateHandler
{
    public static IServiceProvider Services { get; set; } = default!;

    // Simple in-memory onboarding state: which telegram users are expected to send their number next.
    // Note: in Webhook mode with multiple instances this should be moved to durable storage.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<long, byte> AwaitingNumber = new();

    public static async Task HandleUpdate(Update update, TelegramBotClient bot, BotSettings settings, ILogger logger, CancellationToken ct)
    {
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

        // Resolve services from DI.
        await using var scope = Services.CreateAsyncScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

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

        async Task SendOpenWebAppButtonAsync()
        {
            if (string.IsNullOrWhiteSpace(settings.WebAppUrl)) return;

            await bot.SendMessage(
                chatId: update.Message.Chat,
                text: "Tap the button below to open the Mini App.",
                replyMarkup: new InlineKeyboardMarkup(
                    InlineKeyboardButton.WithWebApp(
                        "Open Mini App",
                        settings.WebAppUrl.TrimEnd('/') + "/app")),
                cancellationToken: ct);
        }

        // /start: ensure user exists, and if number isn't stored -> ask for it.
        if (string.Equals(text, "/start", StringComparison.OrdinalIgnoreCase))
        {
            var user = await userService.TouchUserAsync(telegramUserId.Value, ct);

            if (string.IsNullOrWhiteSpace(user.Number))
            {
                await bot.SendMessage(
                    chatId: update.Message.Chat,
                    text: "Welcome! Please send your number (e.g. +123456789).",
                    cancellationToken: ct);

                AwaitingNumber[telegramUserId.Value] = 0;
                return;
            }

            await bot.SendMessage(
                chatId: update.Message.Chat,
                text: "Welcome back!",
                cancellationToken: ct);

            await SendOpenWebAppButtonAsync();
            return;
        }

        // If we're awaiting a number, validate + store it.
        if (AwaitingNumber.TryRemove(telegramUserId.Value, out _))
        {
            var normalized = TryNormalizeNumber(text);
            if (normalized is null)
            {
                await bot.SendMessage(
                    chatId: update.Message.Chat,
                    text: "That doesn't look like a valid number. Please send it again (digits, optional leading +).",
                    cancellationToken: ct);

                AwaitingNumber[telegramUserId.Value] = 0;
                return;
            }

            await userService.SetNumberAsync(telegramUserId.Value, normalized, ct);

            await bot.SendMessage(
                chatId: update.Message.Chat,
                text: "Thank you! Your number has been saved.",
                cancellationToken: ct);

            await SendOpenWebAppButtonAsync();
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

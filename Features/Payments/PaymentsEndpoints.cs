using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MiniApp.Admin;
using MiniApp.Data;
using MiniApp.Features.Auth;
using MiniApp.TelegramLogin;

namespace MiniApp.Features.Payments;

public static class PaymentsEndpoints
{
    public static IEndpointRouteBuilder MapPaymentsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/tonconnect-manifest.json", BuildTonConnectManifestResult);
        endpoints.MapGet("/app/tonconnect-manifest.json", BuildTonConnectManifestResult);

        endpoints.MapGet("/api/admin/payments/tonconnect/diagnostics", [Authorize(Policy = AdminAuth.PolicyName)] async (
            HttpContext http,
            IConfiguration config,
            CancellationToken ct) =>
        {
            var diagnostics = await BuildTonConnectDiagnosticsAsync(http, config, ct);
            return Results.Ok(diagnostics);
        });

        endpoints.MapGet("/api/admin/payments/ton-deposits/diagnostics", [Authorize(Policy = AdminAuth.PolicyName)] async (
            int? limit,
            IPaymentsService payments,
            CancellationToken ct) =>
        {
            var result = await payments.GetTelegramTonAdminDepositDiagnosticsAsync(limit ?? 25, ct);
            if (!result.Success)
                return Results.BadRequest(new { ok = false, error = result.Error ?? "Failed to load TON deposit diagnostics." });

            return Results.Ok(new { ok = true, deposits = result.Deposits });
        });

        endpoints.MapGet("/api/admin/payments/ton-withdrawals/diagnostics", [Authorize(Policy = AdminAuth.PolicyName)] async (
            int? limit,
            AppDbContext db,
            IOptions<PaymentsOptions> options,
            ITelegramTonHotWalletService hotWallet,
            TelegramTonWithdrawalWorkerState workerState,
            CancellationToken ct) =>
        {
            var diagnostics = await BuildTonWithdrawalDiagnosticsAsync(db, options.Value, hotWallet, workerState, limit ?? 25, ct);
            return Results.Ok(new { ok = true, diagnostics });
        });

        endpoints.MapPost("/api/admin/payments/ton-withdrawals/process-next", [Authorize(Policy = AdminAuth.PolicyName)] async (
            int? limit,
            AppDbContext db,
            IOptions<PaymentsOptions> options,
            ITelegramTonHotWalletService hotWallet,
            TelegramTonWithdrawalWorkerState workerState,
            TelegramTonWithdrawalProcessor processor,
            CancellationToken ct) =>
        {
            var changed = await processor.ProcessNextAsync(ct);
            var diagnostics = await BuildTonWithdrawalDiagnosticsAsync(db, options.Value, hotWallet, workerState, limit ?? 25, ct);
            return Results.Ok(new { ok = true, changed, diagnostics });
        });

        endpoints.MapPost("/api/admin/payments/ton-deposits/{depositId:long}/reconcile", [Authorize(Policy = AdminAuth.PolicyName)] async (
            long depositId,
            IPaymentsService payments,
            CancellationToken ct) =>
        {
            var result = await payments.ReconcileTelegramTonAdminDepositAsync(depositId, ct);
            if (!result.Success)
                return Results.BadRequest(new { ok = false, error = result.Error ?? "Failed to reconcile TON deposit." });

            return Results.Ok(new { ok = true, changed = result.Changed, deposit = result.Deposit });
        });

        endpoints.MapGet("/api/payments/systems", (IPaymentsService payments) =>
        {
            var options = payments.GetPaymentSystems();
            return Results.Ok(new { ok = true, options });
        });

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

            var result = await payments.CreateCryptoDepositAsync(user.Id, req.Amount, req.Currency, req.PaymentMethod, ct);
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

    private static IResult BuildTonConnectManifestResult(HttpContext http, IConfiguration config)
    {
        var urls = ResolveTonConnectPublicUrls(http, config);
        http.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        http.Response.Headers.Pragma = "no-cache";

        return Results.Json(new
        {
            url = urls.AppUrl,
            name = "LottoVibe",
            iconUrl = AppendPath(urls.SiteRoot, "img/cafe/Cake_148.png"),
            termsOfUseUrl = AppendPath(urls.SiteRoot, "Privacy"),
            privacyPolicyUrl = AppendPath(urls.SiteRoot, "Privacy")
        });
    }

    private static async Task<TonConnectDiagnosticsView> BuildTonConnectDiagnosticsAsync(
        HttpContext http,
        IConfiguration config,
        CancellationToken ct)
    {
        var configuredBotWebAppUrl = (config["BotWebAppUrl"] ?? string.Empty).Trim();
        var configuredTwaReturnUrl = (config["Payments:TelegramTon:TwaReturnUrl"] ?? string.Empty).Trim();
        var urls = ResolveTonConnectPublicUrls(http, config);
        var request = new TonConnectRequestContextView(
            http.Request.Scheme,
            http.Request.Host.HasValue ? http.Request.Host.Value : string.Empty,
            http.Request.PathBase.Value ?? string.Empty,
            http.Request.Headers["X-Forwarded-Proto"].ToString(),
            http.Request.Headers["X-Forwarded-Host"].ToString());

        var resolved = new TonConnectResolvedUrlsView(
            urls.SiteRoot,
            urls.AppUrl,
            AppendPath(urls.SiteRoot, "tonconnect-manifest.json"),
            AppendPath(urls.AppUrl, "tonconnect-manifest.json"));

        var issues = new List<string>();
        AddUrlIssueIfInvalid(issues, "Configured BotWebAppUrl", configuredBotWebAppUrl, requireHttps: false);
        AddUrlIssueIfInvalid(issues, "Configured Payments:TelegramTon:TwaReturnUrl", configuredTwaReturnUrl, requireHttps: true);
        AddUrlIssueIfInvalid(issues, "Resolved site root", resolved.SiteRoot);
        AddUrlIssueIfInvalid(issues, "Resolved app URL", resolved.AppUrl);
        AddUrlIssueIfInvalid(issues, "Root manifest URL", resolved.RootManifestUrl);
        AddUrlIssueIfInvalid(issues, "App manifest URL", resolved.AppManifestUrl);

        if (string.IsNullOrWhiteSpace(configuredTwaReturnUrl))
        {
            issues.Add("Payments:TelegramTon:TwaReturnUrl is not configured. Telegram Wallet inside Telegram Mini Apps often requires a TMA return URL like https://t.me/<your_bot>.");
        }
        else if (!LooksLikeTelegramReturnUrl(configuredTwaReturnUrl))
        {
            issues.Add("Payments:TelegramTon:TwaReturnUrl does not look like a Telegram deep link. TonConnect UI expects a Telegram return URL such as https://t.me/<your_bot> or tg://resolve?... for TMA wallets.");
        }

        var probes = new List<TonConnectProbeView>();
        var seenProbeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var rootManifestProbe = await ProbeUrlAsync("rootManifest", resolved.RootManifestUrl, ct);
        probes.Add(rootManifestProbe.View);
        var rootManifest = TryParseTonConnectManifest(rootManifestProbe.Body, issues, "root manifest");

        var appManifestProbe = await ProbeUrlAsync("appManifest", resolved.AppManifestUrl, ct);
        probes.Add(appManifestProbe.View);
        var appManifest = TryParseTonConnectManifest(appManifestProbe.Body, issues, "app manifest");

        if (rootManifest is not null && appManifest is not null && !TonConnectManifestEquals(rootManifest, appManifest))
            issues.Add("Root and /app manifest responses are different. Telegram Wallet may be loading a different manifest than expected.");

        var manifests = new[]
        {
            (Label: "rootManifest", Manifest: rootManifest),
            (Label: "appManifest", Manifest: appManifest)
        };

        foreach (var item in manifests)
        {
            if (item.Manifest is null)
                continue;

            AddUrlIssueIfInvalid(issues, item.Label + ".url", item.Manifest.Url);
            AddUrlIssueIfInvalid(issues, item.Label + ".iconUrl", item.Manifest.IconUrl);
            AddUrlIssueIfInvalid(issues, item.Label + ".termsOfUseUrl", item.Manifest.TermsOfUseUrl);
            AddUrlIssueIfInvalid(issues, item.Label + ".privacyPolicyUrl", item.Manifest.PrivacyPolicyUrl);

            await AddProbeIfNeededAsync(seenProbeKeys, probes, item.Label + ".url", item.Manifest.Url, ct);
            await AddProbeIfNeededAsync(seenProbeKeys, probes, item.Label + ".iconUrl", item.Manifest.IconUrl, ct);
            await AddProbeIfNeededAsync(seenProbeKeys, probes, item.Label + ".termsOfUseUrl", item.Manifest.TermsOfUseUrl, ct);
            await AddProbeIfNeededAsync(seenProbeKeys, probes, item.Label + ".privacyPolicyUrl", item.Manifest.PrivacyPolicyUrl, ct);
        }

        foreach (var probe in probes)
        {
            if (!probe.Ok)
                issues.Add($"{probe.Name} probe failed: {probe.Error ?? $"HTTP {(probe.StatusCode?.ToString() ?? "unknown")}"}.");
            else if (probe.StatusCode is < 200 or >= 300)
                issues.Add($"{probe.Name} returned HTTP {probe.StatusCode}. Telegram Wallet may reject redirected or non-success manifest resources.");
            else if (!probe.IsHttps)
                issues.Add($"{probe.Name} resolved to a non-HTTPS URL.");
        }

        return new TonConnectDiagnosticsView(
            DateTimeOffset.UtcNow,
            string.IsNullOrWhiteSpace(configuredBotWebAppUrl) ? null : configuredBotWebAppUrl,
            string.IsNullOrWhiteSpace(configuredTwaReturnUrl) ? null : configuredTwaReturnUrl,
            request,
            resolved,
            issues.Distinct(StringComparer.Ordinal).ToArray(),
            rootManifest,
            appManifest,
            probes);
    }

    private static (string SiteRoot, string AppUrl) ResolveTonConnectPublicUrls(HttpContext http, IConfiguration config)
    {
        var configured = (config["BotWebAppUrl"] ?? string.Empty).Trim();
        if (Uri.TryCreate(configured, UriKind.Absolute, out var configuredUri))
        {
            var configuredUrl = NormalizeAbsoluteUrl(configuredUri);
            var looksLikeExplicitAppUrl = PathEndsWithSegment(configuredUri.AbsolutePath, "app");
            var resolvedSiteRoot = looksLikeExplicitAppUrl
                ? RemoveTrailingPathSegment(configuredUrl, "app")
                : configuredUrl;
            var appUrl = looksLikeExplicitAppUrl
                ? configuredUrl
                : AppendPath(configuredUrl, "app");

            return (resolvedSiteRoot, appUrl);
        }

        var siteRoot = $"{http.Request.Scheme}://{http.Request.Host}{http.Request.PathBase}".TrimEnd('/');
        return (siteRoot, AppendPath(siteRoot, "app"));
    }

    private static string NormalizeAbsoluteUrl(Uri uri)
    {
        var builder = new UriBuilder(uri)
        {
            Query = string.Empty,
            Fragment = string.Empty
        };

        var normalized = builder.Uri.ToString().TrimEnd('/');
        return normalized;
    }

    private static bool PathEndsWithSegment(string? absolutePath, string segment)
    {
        var normalizedPath = (absolutePath ?? string.Empty).TrimEnd('/');
        if (normalizedPath.Length == 0)
            return false;

        return normalizedPath.EndsWith("/" + segment.Trim('/'), StringComparison.OrdinalIgnoreCase);
    }

    private static string RemoveTrailingPathSegment(string url, string segment)
    {
        var trimmedUrl = (url ?? string.Empty).TrimEnd('/');
        var suffix = "/" + segment.Trim('/');
        if (trimmedUrl.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            return trimmedUrl[..^suffix.Length];

        return trimmedUrl;
    }

    private static string AppendPath(string baseUrl, string relativePath)
    {
        var trimmedBase = (baseUrl ?? string.Empty).TrimEnd('/');
        var trimmedPath = (relativePath ?? string.Empty).Trim('/');

        if (string.IsNullOrWhiteSpace(trimmedBase))
            return "/" + trimmedPath;

        if (string.IsNullOrWhiteSpace(trimmedPath))
            return trimmedBase;

        return trimmedBase + "/" + trimmedPath;
    }

    private static async Task AddProbeIfNeededAsync(HashSet<string> seenProbeKeys, List<TonConnectProbeView> probes, string name, string? url, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        var key = name + "|" + url.Trim();
        if (!seenProbeKeys.Add(key))
            return;

        var probe = await ProbeUrlAsync(name, url, ct);
        probes.Add(probe.View);
    }

    private static void AddUrlIssueIfInvalid(List<string> issues, string label, string? url, bool requireHttps = true)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            issues.Add(label + " is empty.");
            return;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            issues.Add(label + " is not an absolute URL: " + url);
            return;
        }

        if (requireHttps && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            issues.Add(label + " is not HTTPS: " + url);
    }

    private static bool LooksLikeTelegramReturnUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        if (string.Equals(uri.Scheme, "tg", StringComparison.OrdinalIgnoreCase))
            return true;

        var host = uri.Host;
        return host.Equals("t.me", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".t.me", StringComparison.OrdinalIgnoreCase);
    }

    private static TonConnectManifestView? TryParseTonConnectManifest(string? body, List<string> issues, string sourceName)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            issues.Add("Unable to parse " + sourceName + ": empty response body.");
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                issues.Add("Unable to parse " + sourceName + ": JSON root is not an object.");
                return null;
            }

            return new TonConnectManifestView(
                ReadJsonString(root, "url"),
                ReadJsonString(root, "name"),
                ReadJsonString(root, "iconUrl"),
                ReadJsonString(root, "termsOfUseUrl"),
                ReadJsonString(root, "privacyPolicyUrl"));
        }
        catch (JsonException ex)
        {
            issues.Add("Unable to parse " + sourceName + ": " + ex.Message);
            return null;
        }
    }

    private static bool TonConnectManifestEquals(TonConnectManifestView left, TonConnectManifestView right)
        => string.Equals(left.Url, right.Url, StringComparison.Ordinal)
           && string.Equals(left.Name, right.Name, StringComparison.Ordinal)
           && string.Equals(left.IconUrl, right.IconUrl, StringComparison.Ordinal)
           && string.Equals(left.TermsOfUseUrl, right.TermsOfUseUrl, StringComparison.Ordinal)
           && string.Equals(left.PrivacyPolicyUrl, right.PrivacyPolicyUrl, StringComparison.Ordinal);

    private static string? ReadJsonString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null)
            return null;

        return property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : property.ToString();
    }

    private static async Task<TonConnectProbeResult> ProbeUrlAsync(string name, string url, CancellationToken ct)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return new TonConnectProbeResult(
                new TonConnectProbeView(name, url, false, null, null, null, null, false, null, "URL is not absolute.", null),
                null);
        }

        try
        {
            using var handler = CreateDiagnosticsHttpHandler();
            using var client = CreateDiagnosticsHttpClient(handler);
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.UserAgent.ParseAdd("MiniApp-TonConnect-Diagnostics/1.0");

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            var finalUrl = response.RequestMessage?.RequestUri?.ToString();
            var redirectLocation = response.Headers.Location?.ToString();
            var contentType = response.Content.Headers.ContentType?.ToString();
            var contentLength = response.Content.Headers.ContentLength;
            var statusCode = (int)response.StatusCode;
            var ok = statusCode >= 200 && statusCode < 300;

            return new TonConnectProbeResult(
                new TonConnectProbeView(
                    name,
                    url,
                    ok,
                    statusCode,
                    contentType,
                    redirectLocation,
                    finalUrl,
                    string.Equals((response.RequestMessage?.RequestUri ?? uri).Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase),
                    contentLength,
                    ok ? null : "HTTP " + statusCode,
                    TruncateBody(body)),
                body);
        }
        catch (Exception ex)
        {
            return new TonConnectProbeResult(
                new TonConnectProbeView(name, url, false, null, null, null, null, string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase), null, ex.Message, null),
                null);
        }
    }

    private static string? TruncateBody(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return body;

        const int maxLength = 800;
        return body.Length <= maxLength
            ? body
            : body[..maxLength] + "…";
    }

    private static HttpClientHandler CreateDiagnosticsHttpHandler()
        => new()
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.All
        };

    private static HttpClient CreateDiagnosticsHttpClient(HttpClientHandler handler)
        => new(handler)
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

    private sealed record TonConnectProbeResult(TonConnectProbeView View, string? Body);

    private static async Task<TonWithdrawalWorkerDiagnosticsView> BuildTonWithdrawalDiagnosticsAsync(
        AppDbContext db,
        PaymentsOptions options,
        ITelegramTonHotWalletService hotWallet,
        TelegramTonWithdrawalWorkerState workerState,
        int limit,
        CancellationToken ct)
    {
        var take = Math.Clamp(limit, 1, 100);
        var telegramTon = options.TelegramTon ?? new TelegramTonOptions();

        var requestRows = await db.WithdrawalRequests
            .AsNoTracking()
            .Include(x => x.User)
            .Where(x => x.AssetCode == WithdrawalAssetCodes.Ton)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(take)
            .Select(x => new
            {
                x.Id,
                x.UserId,
                TelegramUserId = x.User.TelegramUserId,
                AmountUsd = x.Amount,
                AmountTon = x.AssetAmount,
                PayoutAddress = x.Number,
                Status = string.IsNullOrWhiteSpace(x.ExternalPayoutState) ? x.Status.ToString() : x.ExternalPayoutState!,
                ReviewStatus = x.Status.ToString(),
                RawPayoutState = x.ExternalPayoutState,
                x.ExternalPayoutId,
                Memo = x.PayoutMemo,
                AttemptCount = x.PayoutAttemptCount,
                x.PayoutSeqno,
                x.CreatedAtUtc,
                x.ExternalPayoutCreatedAtUtc,
                x.PayoutLastAttemptAtUtc,
                x.PayoutSubmittedAtUtc,
                x.PayoutConfirmedAtUtc,
                LastError = x.PayoutLastError ?? x.ReviewNote
            })
            .ToArrayAsync(ct);

        var requests = requestRows
            .Select(x => new TonWithdrawalRequestDiagnosticView(
                x.Id,
                x.UserId,
                x.TelegramUserId,
                x.AmountUsd,
                x.AmountTon,
                x.PayoutAddress,
                x.Status,
                x.ReviewStatus,
                x.RawPayoutState,
                x.ExternalPayoutId,
                x.Memo,
                x.AttemptCount,
                x.PayoutSeqno,
                x.CreatedAtUtc,
                x.ExternalPayoutCreatedAtUtc,
                x.PayoutLastAttemptAtUtc,
                x.PayoutSubmittedAtUtc,
                x.PayoutConfirmedAtUtc,
                x.LastError,
                BuildWithdrawalRequestIssues(
                    x.ReviewStatus,
                    x.RawPayoutState,
                    x.AmountTon,
                    x.Memo,
                    x.PayoutLastAttemptAtUtc,
                    x.PayoutSubmittedAtUtc,
                    x.LastError,
                    telegramTon)))
            .ToArray();

        var statuses = requests
            .Select(x => (x.Status ?? string.Empty).Trim().ToLowerInvariant())
            .ToArray();

        var hotWalletState = await hotWallet.GetHotWalletStateAsync(ct);
        var worker = workerState.Snapshot();
        var issues = BuildTonWithdrawalWorkerIssues(options, telegramTon, hotWalletState, worker, requests);
        var oldestQueuedRequest = requests
            .Where(x => string.Equals(x.Status, TonWithdrawalPayoutStates.Queued, StringComparison.OrdinalIgnoreCase)
                || string.Equals(x.Status, TonWithdrawalPayoutStates.RetryPending, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.ExternalPayoutCreatedAtUtc ?? x.CreatedAtUtc)
            .FirstOrDefault();
        var queueBlockingReasons = BuildQueueBlockingReasons(options, telegramTon, hotWalletState, worker, oldestQueuedRequest);
        double? oldestQueuedAgeMinutes = oldestQueuedRequest is null
            ? null
            : Math.Round((DateTimeOffset.UtcNow - (oldestQueuedRequest.ExternalPayoutCreatedAtUtc ?? oldestQueuedRequest.CreatedAtUtc)).TotalMinutes, 2);

        return new TonWithdrawalWorkerDiagnosticsView(
            DateTimeOffset.UtcNow,
            options.Enabled,
            telegramTon.Enabled,
            telegramTon.ServerWithdrawalsEnabled,
            TelegramTonNetworkNames.GetConfiguredNetwork(telegramTon),
            telegramTon.ApiBaseUrl,
            hotWalletState,
            worker,
            new TonWithdrawalQueueDiagnosticsView(queueBlockingReasons.Count == 0, queueBlockingReasons, oldestQueuedRequest, oldestQueuedAgeMinutes),
            issues,
            statuses.Count(x => x == TonWithdrawalPayoutStates.Queued),
            statuses.Count(x => x == TonWithdrawalPayoutStates.RetryPending),
            statuses.Count(x => x == TonWithdrawalPayoutStates.Sending),
            statuses.Count(x => x == TonWithdrawalPayoutStates.Submitted),
            statuses.Count(x => x == TonWithdrawalPayoutStates.Confirmed),
            requests);
    }

    private static IReadOnlyList<string> BuildTonWithdrawalWorkerIssues(
        PaymentsOptions options,
        TelegramTonOptions telegramTon,
        TelegramTonHotWalletStateResult hotWalletState,
        TonWithdrawalWorkerRuntimeView worker,
        IReadOnlyList<TonWithdrawalRequestDiagnosticView> requests)
    {
        var issues = new List<string>();
        var queuedCount = requests.Count(x => string.Equals(x.Status, TonWithdrawalPayoutStates.Queued, StringComparison.OrdinalIgnoreCase)
            || string.Equals(x.Status, TonWithdrawalPayoutStates.RetryPending, StringComparison.OrdinalIgnoreCase));

        if (!options.Enabled)
            issues.Add("Payments are disabled, so the TON withdrawal worker will never pick up queued requests.");
        if (!telegramTon.Enabled)
            issues.Add("Telegram TON payments are disabled.");
        if (!telegramTon.ServerWithdrawalsEnabled)
            issues.Add("Server-executed TON withdrawals are disabled.");
        if (string.IsNullOrWhiteSpace(telegramTon.HotWalletMnemonic))
            issues.Add("Hot wallet mnemonic is not configured.");
        if (string.IsNullOrWhiteSpace(telegramTon.HotWalletExpectedAddress))
            issues.Add("Hot wallet expected address is not configured.");

        if (worker.DisabledByConfiguration && !string.IsNullOrWhiteSpace(worker.DisabledReason))
            issues.Add("Worker disabled: " + worker.DisabledReason);

        if (queuedCount > 0)
        {
            if (worker.LastCycleCompletedAtUtc is null)
                issues.Add("Queued withdrawals exist, but the worker has not completed any cycle yet.");

            if (!string.IsNullOrWhiteSpace(worker.LastError))
                issues.Add("Last worker cycle failed: " + worker.LastError);

            if (worker.IntervalSeconds is > 0
                && worker.LastCycleCompletedAtUtc is { } lastCompletedAtUtc
                && lastCompletedAtUtc < DateTimeOffset.UtcNow.AddSeconds(-(worker.IntervalSeconds.Value * 3)))
            {
                issues.Add("Worker heartbeat looks stale. The last completed cycle is older than three worker intervals.");
            }
        }

        if (!hotWalletState.Success)
        {
            issues.Add("Hot wallet diagnostics failed: " + (hotWalletState.Error ?? "Unknown hot wallet error."));
        }
        else
        {
            if (!hotWalletState.IsDeployed)
                issues.Add("Hot wallet is not deployed on-chain.");
            if (hotWalletState.Seqno is null)
                issues.Add("Hot wallet seqno is unavailable.");
            if (!hotWalletState.CanSignTransferProbe)
                issues.Add("Hot wallet transfer-signing probe failed: " + (hotWalletState.TransferProbeError ?? "Unknown signing error."));
        }

        return issues;
    }

    private static List<string> BuildQueueBlockingReasons(
        PaymentsOptions options,
        TelegramTonOptions telegramTon,
        TelegramTonHotWalletStateResult hotWalletState,
        TonWithdrawalWorkerRuntimeView worker,
        TonWithdrawalRequestDiagnosticView? oldestQueuedRequest)
    {
        var blockingReasons = new List<string>();
        if (oldestQueuedRequest is null)
            return blockingReasons;

        if (!options.Enabled)
            blockingReasons.Add("Payments are disabled.");
        if (!telegramTon.Enabled)
            blockingReasons.Add("Telegram TON payments are disabled.");
        if (!telegramTon.ServerWithdrawalsEnabled)
            blockingReasons.Add("Server-executed TON withdrawals are disabled.");
        if (worker.DisabledByConfiguration && !string.IsNullOrWhiteSpace(worker.DisabledReason))
            blockingReasons.Add(worker.DisabledReason);
        if (!worker.Started && !worker.DisabledByConfiguration)
            blockingReasons.Add("Worker has not reported a startup heartbeat yet.");
        if (!string.IsNullOrWhiteSpace(worker.LastError))
            blockingReasons.Add("Last worker cycle failed: " + worker.LastError);

        if (!hotWalletState.Success)
            blockingReasons.Add(hotWalletState.Error ?? "Hot wallet diagnostics failed.");
        else
        {
            if (!hotWalletState.IsDeployed)
                blockingReasons.Add("Hot wallet is not deployed on-chain.");
            if (hotWalletState.Seqno is null)
                blockingReasons.Add("Hot wallet seqno is unavailable.");
            if (!hotWalletState.CanSignTransferProbe)
                blockingReasons.Add(hotWalletState.TransferProbeError ?? "Hot wallet transfer-signing probe failed.");
        }

        blockingReasons.AddRange(oldestQueuedRequest.Issues);
        return blockingReasons
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static IReadOnlyList<string> BuildWithdrawalRequestIssues(
        string reviewStatus,
        string? rawPayoutState,
        decimal? amountTon,
        string? memo,
        DateTimeOffset? lastAttemptAtUtc,
        DateTimeOffset? submittedAtUtc,
        string? lastError,
        TelegramTonOptions telegramTon)
    {
        var issues = new List<string>();
        if (!string.Equals(reviewStatus, WithdrawalRequestStatus.Confirmed.ToString(), StringComparison.OrdinalIgnoreCase))
            issues.Add("Withdrawal is not admin-confirmed yet.");

        if (amountTon is null || amountTon <= 0m)
            issues.Add("TON payout amount has not been calculated yet.");

        if (string.IsNullOrWhiteSpace(memo))
            issues.Add("TON payout memo is missing.");

        var payoutState = (rawPayoutState ?? string.Empty).Trim().ToLowerInvariant();
        if (payoutState == TonWithdrawalPayoutStates.Sending && lastAttemptAtUtc is { } sendingSince)
        {
            if (sendingSince <= DateTimeOffset.UtcNow.AddMinutes(-2))
                issues.Add("Withdrawal has been in sending state for more than 2 minutes and should be recovered by the worker.");
        }

        if (payoutState == TonWithdrawalPayoutStates.Submitted && submittedAtUtc is { } submittedSince)
        {
            var timeoutUtc = submittedSince.AddMinutes(Math.Max(telegramTon.WithdrawalConfirmationTimeoutMinutes, 1));
            if (DateTimeOffset.UtcNow >= timeoutUtc)
                issues.Add("Withdrawal confirmation timeout has elapsed and reconciliation should decide whether to retry.");
        }

        if (!string.IsNullOrWhiteSpace(lastError))
            issues.Add(lastError);

        return issues
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }
}


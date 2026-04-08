using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace MiniApp.Features.Payments;

public sealed class BtcPayClient : IBtcPayClient
{
    private readonly HttpClient _http;
    private readonly PaymentsOptions _options;

    public BtcPayClient(HttpClient http, IOptions<PaymentsOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<BtcPayCreateInvoiceResult> CreateInvoiceAsync(decimal amount, string currency, string orderId, CancellationToken ct)
    {
        if (!TryGetConfiguration(out var btcPay, out var configError))
            return new BtcPayCreateInvoiceResult(false, configError, BtcPayErrorCode.Configuration);

        var url = BuildInvoicesPath(btcPay.StoreId);
        var request = new
        {
            amount = amount,
            currency = currency,
            metadata = new
            {
                orderId = orderId
            }
        };

        var responseResult = await SendWithRetryAsync(() =>
        {
            var message = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(request)
            };
            message.Headers.Authorization = new AuthenticationHeaderValue("token", btcPay.ApiKey.Trim());
            return message;
        }, ct);

        if (!responseResult.Success)
            return new BtcPayCreateInvoiceResult(false, responseResult.Error, responseResult.ErrorCode);

        try
        {
            using var doc = JsonDocument.Parse(responseResult.Body!);
            var root = doc.RootElement;

            var invoiceId = TryGetString(root, "id");
            var checkoutLink = TryGetString(root, "checkoutLink")
                               ?? TryGetNestedString(root, "checkout", "link");
            var expirationRaw = TryGetString(root, "expirationTime");
            var expiration = TryParseBtcPayTime(expirationRaw);

            if (string.IsNullOrWhiteSpace(invoiceId) || string.IsNullOrWhiteSpace(checkoutLink))
                return new BtcPayCreateInvoiceResult(false, "BTCPay response is missing required invoice fields.", BtcPayErrorCode.Parse);

            return new BtcPayCreateInvoiceResult(true, null, BtcPayErrorCode.None, invoiceId, checkoutLink, expiration);
        }
        catch
        {
            return new BtcPayCreateInvoiceResult(false, "Failed to parse BTCPay invoice response.", BtcPayErrorCode.Parse);
        }
    }

    public async Task<BtcPayGetInvoiceResult> GetInvoiceAsync(string invoiceId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(invoiceId))
            return new BtcPayGetInvoiceResult(false, "Invoice id is required.", BtcPayErrorCode.InvalidRequest);

        if (!TryGetConfiguration(out var btcPay, out var configError))
            return new BtcPayGetInvoiceResult(false, configError, BtcPayErrorCode.Configuration);

        var url = BuildInvoicePath(btcPay.StoreId, invoiceId.Trim());
        var responseResult = await SendWithRetryAsync(() =>
        {
            var message = new HttpRequestMessage(HttpMethod.Get, url);
            message.Headers.Authorization = new AuthenticationHeaderValue("token", btcPay.ApiKey.Trim());
            return message;
        }, ct);

        if (!responseResult.Success)
            return new BtcPayGetInvoiceResult(false, responseResult.Error, responseResult.ErrorCode);

        try
        {
            using var doc = JsonDocument.Parse(responseResult.Body!);
            var root = doc.RootElement;

            var returnedInvoiceId = TryGetString(root, "id");
            var status = TryGetString(root, "status");
            var amount = TryGetDecimal(root, "amount");
            var currency = TryGetString(root, "currency");
            var checkoutLink = TryGetString(root, "checkoutLink")
                               ?? TryGetNestedString(root, "checkout", "link");
            var expiration = TryParseBtcPayTime(TryGetString(root, "expirationTime"));

            return new BtcPayGetInvoiceResult(
                true,
                null,
                BtcPayErrorCode.None,
                returnedInvoiceId,
                status,
                amount,
                currency,
                checkoutLink,
                expiration);
        }
        catch
        {
            return new BtcPayGetInvoiceResult(false, "Failed to parse BTCPay invoice response.", BtcPayErrorCode.Parse);
        }
    }

    public async Task<BtcPayCreatePayoutResult> CreatePayoutAsync(BtcPayCreatePayoutRequest request, CancellationToken ct)
    {
        if (request is null)
            return new BtcPayCreatePayoutResult(false, "Payout request is required.", BtcPayErrorCode.InvalidRequest);

        if (request.Amount <= 0m)
            return new BtcPayCreatePayoutResult(false, "Payout amount must be greater than zero.", BtcPayErrorCode.InvalidRequest);

        if (string.IsNullOrWhiteSpace(request.Destination))
            return new BtcPayCreatePayoutResult(false, "Destination address is required.", BtcPayErrorCode.InvalidRequest);

        if (!TryGetConfiguration(out var btcPay, out var configError))
            return new BtcPayCreatePayoutResult(false, configError, BtcPayErrorCode.Configuration);

        var pullPaymentId = NormalizePullPaymentId(btcPay.WithdrawalsPullPaymentId);
        if (string.IsNullOrWhiteSpace(pullPaymentId))
            return new BtcPayCreatePayoutResult(false, "BTCPay withdrawals pull payment id is not configured.", BtcPayErrorCode.Configuration);

        var url = BuildPullPaymentPayoutsPath(btcPay.StoreId, pullPaymentId);
        var payoutMethod = string.IsNullOrWhiteSpace(btcPay.WithdrawalsPaymentMethod) ? "BTC-CHAIN" : btcPay.WithdrawalsPaymentMethod.Trim();
        var normalizedCurrency = string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency.Trim().ToUpperInvariant();
        var basePayload = new
        {
            amount = request.Amount,
            currency = normalizedCurrency,
            destination = request.Destination.Trim(),
            notificationUrl = string.IsNullOrWhiteSpace(request.NotificationUrl) ? null : request.NotificationUrl.Trim(),
            metadata = string.IsNullOrWhiteSpace(request.Reference)
                ? null
                : new { reference = request.Reference.Trim() }
        };

        async Task<BtcPayHttpResult> SendPayoutAsync(string endpoint, object payload)
            => await SendWithRetryAsync(() =>
            {
                var message = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = JsonContent.Create(payload)
                };
                message.Headers.Authorization = new AuthenticationHeaderValue("token", btcPay.ApiKey.Trim());
                return message;
            }, ct);

        var responseResult = await SendPayoutAsync(url, new
        {
            amount = basePayload.amount,
            currency = basePayload.currency,
            destination = basePayload.destination,
            paymentMethod = payoutMethod,
            notificationUrl = basePayload.notificationUrl,
            metadata = basePayload.metadata
        });

        // Some BTCPay versions validate payout payload shape differently; retry compatible variants on 422.
        if (!responseResult.Success && responseResult.ErrorCode == BtcPayErrorCode.InvalidRequest)
        {
            responseResult = await SendPayoutAsync(url, new
            {
                amount = basePayload.amount,
                currency = basePayload.currency,
                destination = basePayload.destination,
                paymentMethodId = payoutMethod,
                notificationUrl = basePayload.notificationUrl,
                metadata = basePayload.metadata
            });
        }

        if (!responseResult.Success && responseResult.ErrorCode == BtcPayErrorCode.InvalidRequest)
        {
            responseResult = await SendPayoutAsync(url, new
            {
                amount = basePayload.amount,
                currency = basePayload.currency,
                destination = basePayload.destination,
                notificationUrl = basePayload.notificationUrl,
                metadata = basePayload.metadata
            });
        }

        // Some BTCPay versions expose payout creation at /stores/{storeId}/payouts.
        if (!responseResult.Success && responseResult.ErrorCode == BtcPayErrorCode.NotFound)
        {
            var fallbackUrl = BuildStorePayoutsPath(btcPay.StoreId);
            var fallbackPayload = new
            {
                pullPaymentId,
                amount = basePayload.amount,
                currency = basePayload.currency,
                destination = basePayload.destination,
                paymentMethod = payoutMethod,
                paymentMethodId = payoutMethod,
                notificationUrl = basePayload.notificationUrl,
                metadata = basePayload.metadata
            };

            responseResult = await SendPayoutAsync(fallbackUrl, fallbackPayload);

            if (!responseResult.Success && responseResult.ErrorCode == BtcPayErrorCode.InvalidRequest)
            {
                responseResult = await SendPayoutAsync(fallbackUrl, new
                {
                    pullPaymentId,
                    amount = basePayload.amount,
                    currency = basePayload.currency,
                    destination = basePayload.destination,
                    paymentMethod = payoutMethod,
                    notificationUrl = basePayload.notificationUrl,
                    metadata = basePayload.metadata
                });
            }
        }

        if (!responseResult.Success)
            return new BtcPayCreatePayoutResult(false, responseResult.Error, responseResult.ErrorCode);

        try
        {
            using var doc = JsonDocument.Parse(responseResult.Body!);
            var root = doc.RootElement;

            var payoutId = TryGetString(root, "id");
            var state = TryGetString(root, "state");
            var responsePullPaymentId = TryGetString(root, "pullPaymentId") ?? pullPaymentId;
            var createdAt = TryParseBtcPayTime(TryGetString(root, "date"))
                ?? TryParseBtcPayTime(TryGetString(root, "createdTime"));

            if (string.IsNullOrWhiteSpace(payoutId))
                return new BtcPayCreatePayoutResult(false, "BTCPay response is missing payout id.", BtcPayErrorCode.Parse);

            return new BtcPayCreatePayoutResult(
                true,
                null,
                BtcPayErrorCode.None,
                payoutId,
                state,
                responsePullPaymentId,
                createdAt);
        }
        catch
        {
            return new BtcPayCreatePayoutResult(false, "Failed to parse BTCPay payout response.", BtcPayErrorCode.Parse);
        }
    }

    private bool TryGetConfiguration(out BtcPayOptions options, out string? error)
    {
        options = _options.BtcPay;
        if (string.IsNullOrWhiteSpace(options.BaseUrl)
            || string.IsNullOrWhiteSpace(options.StoreId)
            || string.IsNullOrWhiteSpace(options.ApiKey))
        {
            error = "BTCPay is not configured.";
            return false;
        }

        error = null;
        return true;
    }

    private static string BuildInvoicesPath(string storeId)
        => $"{BuildStorePath(storeId)}/invoices";

    private static string BuildInvoicePath(string storeId, string invoiceId)
        => $"{BuildStorePath(storeId)}/invoices/{Uri.EscapeDataString(invoiceId)}";

    private static string BuildPullPaymentPayoutsPath(string storeId, string pullPaymentId)
        => $"{BuildStorePath(storeId)}/pull-payments/{Uri.EscapeDataString(pullPaymentId)}/payouts";

    private static string BuildStorePayoutsPath(string storeId)
        => $"{BuildStorePath(storeId)}/payouts";

    private static string BuildStorePath(string storeId)
        => $"{NormalizeApiBasePath()}/stores/{Uri.EscapeDataString(storeId)}";

    private static string NormalizeApiBasePath()
        => "api/v1";

    private async Task<BtcPayHttpResult> SendWithRetryAsync(Func<HttpRequestMessage> buildRequest, CancellationToken ct)
    {
        var maxAttempts = Math.Max(_options.BtcPay.MaxRetryAttempts, 1);
        var delay = TimeSpan.FromMilliseconds(200);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var request = buildRequest();
                request.RequestUri = BuildAbsoluteUri(request.RequestUri);

                using var response = await _http.SendAsync(request, ct);
                var body = await response.Content.ReadAsStringAsync(ct);

                if (response.IsSuccessStatusCode)
                    return BtcPayHttpResult.FromSuccess(body);

                var errorCode = MapErrorCode(response.StatusCode);
                if (ShouldRetry(response.StatusCode) && attempt < maxAttempts)
                {
                    await Task.Delay(delay, ct);
                    delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2);
                    continue;
                }

                return BtcPayHttpResult.FromFailure(errorCode, BuildHttpErrorMessage(response.StatusCode, body));
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                if (attempt < maxAttempts)
                {
                    await Task.Delay(delay, ct);
                    delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2);
                    continue;
                }

                return BtcPayHttpResult.FromFailure(BtcPayErrorCode.Transient, "BTCPay request timed out.");
            }
            catch (HttpRequestException)
            {
                if (attempt < maxAttempts)
                {
                    await Task.Delay(delay, ct);
                    delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2);
                    continue;
                }

                return BtcPayHttpResult.FromFailure(BtcPayErrorCode.Transient, "BTCPay request failed due to network error.");
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return BtcPayHttpResult.FromFailure(BtcPayErrorCode.Canceled, "BTCPay request was canceled.");
            }
        }

        return BtcPayHttpResult.FromFailure(BtcPayErrorCode.Upstream, "BTCPay request failed.");
    }

    private Uri BuildAbsoluteUri(Uri? requestUri)
    {
        if (requestUri is not null && requestUri.IsAbsoluteUri)
            return requestUri;

        var baseUrl = _options.BtcPay.BaseUrl?.TrimEnd('/') ?? string.Empty;
        var relative = requestUri?.ToString().TrimStart('/') ?? string.Empty;
        return new Uri($"{baseUrl}/{relative}", UriKind.Absolute);
    }

    private static bool ShouldRetry(HttpStatusCode statusCode)
        => (int)statusCode >= 500 || statusCode == HttpStatusCode.TooManyRequests;

    private static BtcPayErrorCode MapErrorCode(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.BadRequest => BtcPayErrorCode.InvalidRequest,
            HttpStatusCode.UnprocessableEntity => BtcPayErrorCode.InvalidRequest,
            HttpStatusCode.Unauthorized => BtcPayErrorCode.Unauthorized,
            HttpStatusCode.Forbidden => BtcPayErrorCode.Unauthorized,
            HttpStatusCode.NotFound => BtcPayErrorCode.NotFound,
            HttpStatusCode.TooManyRequests => BtcPayErrorCode.RateLimited,
            _ when (int)statusCode >= 500 => BtcPayErrorCode.Transient,
            _ => BtcPayErrorCode.Upstream
        };
    }

    private static string BuildHttpErrorMessage(HttpStatusCode statusCode, string? body = null)
    {
        var suffix = ExtractErrorSuffix(body);
        return string.IsNullOrWhiteSpace(suffix)
            ? $"BTCPay request failed ({(int)statusCode})."
            : $"BTCPay request failed ({(int)statusCode}): {suffix}";
    }

    private static string? ExtractErrorSuffix(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.String)
                return TruncateErrorText(root.GetString());

            if (TryGetString(root, "message") is { Length: > 0 } message)
                return TruncateErrorText(message);

            if (TryGetString(root, "error") is { Length: > 0 } error)
                return TruncateErrorText(error);

            if (root.TryGetProperty("errors", out var errorsProp) && errorsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in errorsProp.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                        return TruncateErrorText(item.GetString());

                    if (item.ValueKind == JsonValueKind.Object
                        && TryGetString(item, "message") is { Length: > 0 } nestedMessage)
                    {
                        return TruncateErrorText(nestedMessage);
                    }
                }
            }
        }
        catch
        {
            // Non-JSON response body.
        }

        return TruncateErrorText(body);
    }

    private static string? TruncateErrorText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return trimmed.Length <= 300 ? trimmed : trimmed[..300];
    }

    private static string? TryGetString(JsonElement root, string name)
    {
        if (root.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
            return prop.GetString();
        return null;
    }

    private static string? TryGetNestedString(JsonElement root, string obj, string name)
    {
        if (!root.TryGetProperty(obj, out var parent) || parent.ValueKind != JsonValueKind.Object)
            return null;
        if (!parent.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String)
            return null;
        return value.GetString();
    }

    private static decimal? TryGetDecimal(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var prop))
            return null;

        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetDecimal(out var value))
            return value;

        if (prop.ValueKind == JsonValueKind.String
            && decimal.TryParse(prop.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out value))
            return value;

        return null;
    }

    private static DateTimeOffset? TryParseBtcPayTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var parsed))
            return parsed;

        return null;
    }

    private static string? NormalizePullPaymentId(string? raw)
    {
        var value = raw?.Trim();
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            var query = uri.Query;
            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in q)
                {
                    var kv = part.Split('=', 2);
                    if (kv.Length == 2 && string.Equals(kv[0], "pullPaymentId", StringComparison.OrdinalIgnoreCase))
                    {
                        var idFromQuery = Uri.UnescapeDataString(kv[1]).Trim();
                        if (!string.IsNullOrWhiteSpace(idFromQuery))
                            return idFromQuery;
                    }
                }
            }

            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < segments.Length; i++)
            {
                if (!string.Equals(segments[i], "pull-payments", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (i + 1 < segments.Length)
                {
                    var idFromPath = Uri.UnescapeDataString(segments[i + 1]).Trim();
                    if (!string.IsNullOrWhiteSpace(idFromPath))
                        return idFromPath;
                }
            }

            var last = uri.Segments.LastOrDefault();
            if (!string.IsNullOrWhiteSpace(last))
            {
                var idFromLast = Uri.UnescapeDataString(last).Trim('/').Trim();
                if (!string.IsNullOrWhiteSpace(idFromLast))
                    return idFromLast;
            }
        }

        return value;
    }

    private sealed record BtcPayHttpResult(bool Success, string? Body, BtcPayErrorCode ErrorCode, string? Error)
    {
        public static BtcPayHttpResult FromSuccess(string body)
            => new(true, body, BtcPayErrorCode.None, null);

        public static BtcPayHttpResult FromFailure(BtcPayErrorCode errorCode, string error)
            => new(false, null, errorCode, error);
    }
}


using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace MiniApp.Features.Payments;

public sealed class TelegramTonClient : ITelegramTonClient
{
	private const string TonRateCoinId = "the-open-network";
	private readonly HttpClient _http;
	private readonly PaymentsOptions _options;

	public TelegramTonClient(HttpClient http, IOptions<PaymentsOptions> options)
	{
		_http = http;
		_options = options.Value;
	}

	public async Task<TelegramTonLookupResult> TryFindIncomingTransferAsync(TelegramTonLookupRequest request, CancellationToken ct)
	{
		var walletAddress = request.WalletAddress.Trim();
		var referenceMemo = request.ReferenceMemo.Trim();
		if (walletAddress.Length == 0 || referenceMemo.Length == 0)
			return new TelegramTonLookupResult(false, false, "TON wallet address and reference memo are required.");

		var baseUrl = _options.TelegramTon.ApiBaseUrl.Trim();
		if (baseUrl.Length == 0)
			baseUrl = "https://toncenter.com/api/v2/";
		if (!baseUrl.EndsWith("/", StringComparison.Ordinal))
			baseUrl += "/";

		var limit = Math.Clamp(request.SearchLimit, 1, 100);
		var requestUri = new Uri(baseUrl + "getTransactions?address=" + Uri.EscapeDataString(walletAddress) + "&limit=" + limit + "&archival=true", UriKind.Absolute);

		try
		{
			using var message = new HttpRequestMessage(HttpMethod.Get, requestUri);
			var apiKey = _options.TelegramTon.ApiKey.Trim();
			if (apiKey.Length > 0)
				message.Headers.Add("X-API-Key", apiKey);

			using var response = await _http.SendAsync(message, ct);
			var body = await response.Content.ReadAsStringAsync(ct);
			if (!response.IsSuccessStatusCode)
				return new TelegramTonLookupResult(false, false, $"TON lookup failed ({(int)response.StatusCode}).");

			using var doc = JsonDocument.Parse(body);
			var root = doc.RootElement;
			if (root.ValueKind != JsonValueKind.Object)
				return new TelegramTonLookupResult(false, false, "Unexpected TON response format.");

			if (root.TryGetProperty("ok", out var okProp)
				&& okProp.ValueKind == JsonValueKind.False)
			{
				return new TelegramTonLookupResult(false, false, ExtractError(root) ?? "TON API returned an error.");
			}

			if (!root.TryGetProperty("result", out var resultProp) || resultProp.ValueKind != JsonValueKind.Array)
				return new TelegramTonLookupResult(false, false, "TON API response does not contain transactions.");

			foreach (var tx in resultProp.EnumerateArray())
			{
				if (tx.ValueKind != JsonValueKind.Object)
					continue;

				var observedAtUtc = TryGetTransactionTime(tx);
				if (observedAtUtc is not null && observedAtUtc.Value < request.CreatedAfterUtc.AddMinutes(-3))
					continue;

				if (!tx.TryGetProperty("in_msg", out var inMsg) || inMsg.ValueKind != JsonValueKind.Object)
					continue;

				var memo = ExtractMessageText(inMsg);
				if (!string.Equals(memo?.Trim(), referenceMemo, StringComparison.Ordinal))
					continue;

				var valueNanotons = TryGetDecimal(inMsg, "value");
				if (valueNanotons is null)
					continue;

				var valueTon = decimal.Round(valueNanotons.Value / 1_000_000_000m, 8, MidpointRounding.AwayFromZero);
				if (valueTon + 0.00000001m < request.ExpectedTonAmount)
					continue;

				var transactionId = ExtractTransactionId(tx);
				return new TelegramTonLookupResult(
					true,
					true,
					null,
					transactionId,
					valueTon,
					observedAtUtc,
					BuildExplorerLink(request.ExplorerBaseUrl, transactionId),
					TryGetString(inMsg, "source"));
			}

			return new TelegramTonLookupResult(true, false);
		}
		catch (OperationCanceledException) when (!ct.IsCancellationRequested)
		{
			return new TelegramTonLookupResult(false, false, "TON lookup timed out.");
		}
		catch (Exception ex)
		{
			return new TelegramTonLookupResult(false, false, ex.Message);
		}
	}

	public async Task<TelegramTonUsdRateResult> GetUsdPerTonRateAsync(CancellationToken ct)
	{
		var baseUrl = _options.TelegramTon.RateApiBaseUrl.Trim();
		if (baseUrl.Length == 0)
			baseUrl = "https://api.coingecko.com/api/v3/";
		if (!baseUrl.EndsWith("/", StringComparison.Ordinal))
			baseUrl += "/";

		var requestUri = new Uri(baseUrl + "simple/price?ids=" + Uri.EscapeDataString(TonRateCoinId) + "&vs_currencies=usd", UriKind.Absolute);

		try
		{
			using var message = new HttpRequestMessage(HttpMethod.Get, requestUri);
			var apiKey = _options.TelegramTon.RateApiKey.Trim();
			if (apiKey.Length > 0)
			{
				message.Headers.Add("x-cg-demo-api-key", apiKey);
				message.Headers.Add("x-cg-pro-api-key", apiKey);
			}

			using var response = await _http.SendAsync(message, ct);
			var body = await response.Content.ReadAsStringAsync(ct);
			if (!response.IsSuccessStatusCode)
				return new TelegramTonUsdRateResult(false, $"TON rate lookup failed ({(int)response.StatusCode}).");

			using var doc = JsonDocument.Parse(body);
			var root = doc.RootElement;
			if (root.ValueKind != JsonValueKind.Object)
				return new TelegramTonUsdRateResult(false, "Unexpected TON rate response format.");

			if (!root.TryGetProperty(TonRateCoinId, out var tonNode) || tonNode.ValueKind != JsonValueKind.Object)
				return new TelegramTonUsdRateResult(false, "TON rate response does not contain TON market data.");

			var usdPerTon = TryGetDecimal(tonNode, "usd");
			if (usdPerTon is null || usdPerTon <= 0m)
				return new TelegramTonUsdRateResult(false, "TON rate response does not contain a valid USD price.");

			return new TelegramTonUsdRateResult(
				true,
				null,
				new TelegramTonUsdRateQuote(
					usdPerTon.Value,
					DateTimeOffset.UtcNow,
					"CoinGecko"));
		}
		catch (OperationCanceledException) when (!ct.IsCancellationRequested)
		{
			return new TelegramTonUsdRateResult(false, "TON rate lookup timed out.");
		}
		catch (Exception ex)
		{
			return new TelegramTonUsdRateResult(false, ex.Message);
		}
	}

	private static string? ExtractError(JsonElement root)
		=> TryGetString(root, "error") ?? TryGetString(root, "description");

	private static string? ExtractMessageText(JsonElement inMsg)
	{
		if (TryGetString(inMsg, "message") is { Length: > 0 } message) return message;
		if (TryGetString(inMsg, "comment") is { Length: > 0 } comment) return comment;
		if (TryGetString(inMsg, "text") is { Length: > 0 } text) return text;

		if (inMsg.TryGetProperty("msg_data", out var msgData) && msgData.ValueKind == JsonValueKind.Object)
		{
			if (TryGetString(msgData, "text") is { Length: > 0 } nestedText)
			{
				if (TryDecodeTonBytesToText(nestedText) is { Length: > 0 } decodedNestedText) return decodedNestedText;
				return nestedText;
			}

			if (TryGetString(msgData, "comment") is { Length: > 0 } nestedComment)
			{
				if (TryDecodeTonBytesToText(nestedComment) is { Length: > 0 } decodedNestedComment) return decodedNestedComment;
				return nestedComment;
			}

			if (TryExtractCommentFromRawBody(msgData) is { Length: > 0 } rawComment) return rawComment;
		}

		return null;
	}

	private static string? TryExtractCommentFromRawBody(JsonElement msgData)
	{
		if (TryGetString(msgData, "body") is not { Length: > 0 } body)
			return null;

		try
		{
			var payload = Convert.FromBase64String(body);
			if (payload.Length >= 4
				&& payload[0] == 0
				&& payload[1] == 0
				&& payload[2] == 0
				&& payload[3] == 0)
			{
				var comment = System.Text.Encoding.UTF8.GetString(payload, 4, payload.Length - 4).TrimEnd('\0');
				return string.IsNullOrWhiteSpace(comment) ? null : comment;
			}

			var decoded = System.Text.Encoding.UTF8.GetString(payload).TrimEnd('\0');
			return string.IsNullOrWhiteSpace(decoded) ? null : decoded;
		}
		catch
		{
			return null;
		}
	}

	private static string? TryDecodeTonBytesToText(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return null;

		try
		{
			var bytes = Convert.FromBase64String(value);
			var decoded = System.Text.Encoding.UTF8.GetString(bytes).TrimEnd('\0');
			return string.IsNullOrWhiteSpace(decoded) ? null : decoded;
		}
		catch
		{
			return null;
		}
	}

	private static DateTimeOffset? TryGetTransactionTime(JsonElement tx)
	{
		if (!tx.TryGetProperty("utime", out var utimeProp))
			return null;

		if (utimeProp.ValueKind == JsonValueKind.Number && utimeProp.TryGetInt64(out var unixSeconds))
			return DateTimeOffset.FromUnixTimeSeconds(unixSeconds);

		if (utimeProp.ValueKind == JsonValueKind.String
			&& long.TryParse(utimeProp.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out unixSeconds))
		{
			return DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
		}

		return null;
	}

	private static string? ExtractTransactionId(JsonElement tx)
	{
		if (tx.TryGetProperty("transaction_id", out var txId) && txId.ValueKind == JsonValueKind.Object)
		{
			if (TryGetString(txId, "hash") is { Length: > 0 } hash)
				return hash;
		}

		return TryGetString(tx, "hash") ?? TryGetString(tx, "id");
	}

	private static string? BuildExplorerLink(string? explorerBaseUrl, string? transactionId)
	{
		var txId = (transactionId ?? string.Empty).Trim();
		var baseUrl = (explorerBaseUrl ?? string.Empty).Trim();
		if (txId.Length == 0 || baseUrl.Length == 0)
			return null;

		if (!baseUrl.EndsWith("/", StringComparison.Ordinal))
			baseUrl += "/";

		return baseUrl + Uri.EscapeDataString(txId);
	}

	private static string? TryGetString(JsonElement root, string name)
	{
		if (root.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
			return prop.GetString();

		return null;
	}

	private static decimal? TryGetDecimal(JsonElement root, string name)
	{
		if (!root.TryGetProperty(name, out var prop))
			return null;

		if (prop.ValueKind == JsonValueKind.Number && prop.TryGetDecimal(out var numeric))
			return numeric;

		if (prop.ValueKind == JsonValueKind.String
			&& decimal.TryParse(prop.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out numeric))
		{
			return numeric;
		}

		return null;
	}
}



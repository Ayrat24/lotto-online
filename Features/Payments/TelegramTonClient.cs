using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TonSdk.Core.Boc;
using TonSdk.Core;

namespace MiniApp.Features.Payments;

public sealed class TelegramTonClient : ITelegramTonClient
{
	private const string TonRateCoinId = "the-open-network";
	private static readonly ConcurrentDictionary<string, CachedTransactionBatch> TransactionCache = new(StringComparer.Ordinal);
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

		var expectedNanotons = ToNanotons(request.ExpectedTonAmount);
		var toleranceNanotons = ToNanotons(GetDepositMatchToleranceTon());
		var baseUrl = GetNormalizedApiBaseUrl();

		var limit = Math.Clamp(request.SearchLimit, 1, 100);

		try
		{
			var batch = await GetTransactionsAsync(walletAddress, baseUrl, limit, ct);
			if (!batch.Success)
				return new TelegramTonLookupResult(false, false, batch.Error ?? "TON lookup failed.");

			foreach (var tx in batch.Transactions)
			{
				var observedAtUtc = tx.ObservedAtUtc;
				if (observedAtUtc is not null && observedAtUtc.Value < request.CreatedAfterUtc.AddMinutes(-3))
					continue;

				if (!string.Equals(tx.Memo?.Trim(), referenceMemo, StringComparison.Ordinal))
					continue;

				if (tx.ValueNanotons is null)
					continue;

				var valueTon = decimal.Round(tx.ValueNanotons.Value / 1_000_000_000m, 8, MidpointRounding.AwayFromZero);
				if (tx.ValueNanotons.Value + toleranceNanotons < expectedNanotons)
					continue;

				return new TelegramTonLookupResult(
					true,
					true,
					null,
					tx.TransactionId,
					valueTon,
					observedAtUtc,
					BuildExplorerLink(request.ExplorerBaseUrl, tx.TransactionId),
					tx.SenderAddress);
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

	public async Task<TelegramTonRecentTransfersResult> GetRecentIncomingTransfersAsync(TelegramTonRecentTransfersRequest request, CancellationToken ct)
	{
		var walletAddress = request.WalletAddress.Trim();
		if (walletAddress.Length == 0)
			return new TelegramTonRecentTransfersResult(false, "TON wallet address is required.");

		var baseUrl = GetNormalizedApiBaseUrl();

		var limit = Math.Clamp(request.SearchLimit, 1, 100);

		try
		{
			var batch = await GetTransactionsAsync(walletAddress, baseUrl, limit, ct);
			if (!batch.Success)
				return new TelegramTonRecentTransfersResult(false, batch.Error ?? "TON lookup failed.");

			var transfers = batch.Transactions
				.OrderByDescending(tx => tx.ObservedAtUtc ?? DateTimeOffset.MinValue)
				.Select(tx => new TelegramTonIncomingTransferView(
					tx.TransactionId,
					tx.ValueNanotons is null ? null : decimal.Round(tx.ValueNanotons.Value / 1_000_000_000m, 8, MidpointRounding.AwayFromZero),
					tx.ObservedAtUtc,
					BuildExplorerLink(request.ExplorerBaseUrl, tx.TransactionId),
					tx.SenderAddress,
					tx.Memo))
				.ToArray();

			return new TelegramTonRecentTransfersResult(true, null, transfers, batch.RawTransactionCount);
		}
		catch (OperationCanceledException) when (!ct.IsCancellationRequested)
		{
			return new TelegramTonRecentTransfersResult(false, "TON lookup timed out.");
		}
		catch (Exception ex)
		{
			return new TelegramTonRecentTransfersResult(false, ex.Message);
		}
	}

	private decimal GetDepositMatchToleranceTon()
		=> decimal.Clamp(
			_options.TelegramTon.DepositMatchToleranceTon,
			0m,
			TelegramTonOptions.MaxDepositMatchToleranceTon);

	private string GetNormalizedApiBaseUrl()
	{
		var baseUrl = _options.TelegramTon.ApiBaseUrl.Trim();
		if (baseUrl.Length == 0)
			baseUrl = "https://toncenter.com/api/v2/";
		if (!baseUrl.EndsWith("/", StringComparison.Ordinal))
			baseUrl += "/";

		return baseUrl;
	}

	private async Task<CachedTransactionBatch> GetTransactionsAsync(string walletAddress, string baseUrl, int limit, CancellationToken ct)
	{
		CachedTransactionBatch? firstSuccess = null;
		CachedTransactionBatch? lastFailure = null;

		foreach (var addressCandidate in BuildWalletAddressCandidates(walletAddress))
		{
			foreach (var archival in GetArchivalModes())
			{
				var batch = await GetTransactionsForAddressVariantAsync(addressCandidate, baseUrl, limit, archival, ct);
				if (!batch.Success)
				{
					lastFailure = batch;
					continue;
				}

				firstSuccess ??= batch;
				if (batch.RawTransactionCount > 0)
					return batch;
			}
		}

		return firstSuccess
			?? lastFailure
			?? new CachedTransactionBatch(false, Array.Empty<TelegramTonTransactionCandidate>(), "TON lookup failed.", 0, DateTimeOffset.UtcNow.AddSeconds(5));
	}

	private async Task<CachedTransactionBatch> GetTransactionsForAddressVariantAsync(string walletAddress, string baseUrl, int limit, bool archival, CancellationToken ct)
	{
		var now = DateTimeOffset.UtcNow;
		var cacheKey = string.Create(CultureInfo.InvariantCulture, $"{baseUrl}|{walletAddress.Trim()}|{limit}|{archival}");
		if (TransactionCache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAtUtc > now)
			return cached;

		var requestPath = baseUrl + "getTransactions?address=" + Uri.EscapeDataString(walletAddress) + "&limit=" + limit;
		if (archival)
			requestPath += "&archival=true";

		var requestUri = new Uri(requestPath, UriKind.Absolute);
		using var message = new HttpRequestMessage(HttpMethod.Get, requestUri);
		var apiKey = _options.TelegramTon.ApiKey.Trim();
		if (apiKey.Length > 0)
			message.Headers.Add("X-API-Key", apiKey);

		using var response = await _http.SendAsync(message, ct);
		var body = await response.Content.ReadAsStringAsync(ct);
		if (!response.IsSuccessStatusCode)
		{
			var ttl = response.StatusCode == System.Net.HttpStatusCode.TooManyRequests
				? TimeSpan.FromSeconds(15)
				: TimeSpan.FromSeconds(5);
			var failure = new CachedTransactionBatch(
				false,
				Array.Empty<TelegramTonTransactionCandidate>(),
				$"TON lookup failed ({(int)response.StatusCode}).",
				0,
				now.Add(ttl));
			TransactionCache[cacheKey] = failure;
			return failure;
		}

		using var doc = JsonDocument.Parse(body);
		var root = doc.RootElement;
		if (root.ValueKind != JsonValueKind.Object)
			return CacheFailure(cacheKey, now, "Unexpected TON response format.");

		if (root.TryGetProperty("ok", out var okProp)
			&& okProp.ValueKind == JsonValueKind.False)
		{
			return CacheFailure(cacheKey, now, ExtractError(root) ?? "TON API returned an error.");
		}

		if (!root.TryGetProperty("result", out var resultProp) || resultProp.ValueKind != JsonValueKind.Array)
			return CacheFailure(cacheKey, now, "TON API response does not contain transactions.");

		var rawTransactionCount = resultProp.GetArrayLength();
		var transactions = new List<TelegramTonTransactionCandidate>();
		foreach (var tx in resultProp.EnumerateArray())
		{
			if (tx.ValueKind != JsonValueKind.Object)
				continue;

			if (!tx.TryGetProperty("in_msg", out var inMsg) || inMsg.ValueKind != JsonValueKind.Object)
				continue;

			transactions.Add(new TelegramTonTransactionCandidate(
				ExtractTransactionId(tx),
				TryGetTransactionTime(tx),
				ExtractMessageText(inMsg),
				TryGetDecimal(inMsg, "value"),
				TryGetString(inMsg, "source")));
		}

		var success = new CachedTransactionBatch(true, transactions, null, rawTransactionCount, now.AddSeconds(5));
		TransactionCache[cacheKey] = success;
		return success;
	}

	private static CachedTransactionBatch CacheFailure(string cacheKey, DateTimeOffset now, string error)
	{
		var failure = new CachedTransactionBatch(false, Array.Empty<TelegramTonTransactionCandidate>(), error, 0, now.AddSeconds(5));
		TransactionCache[cacheKey] = failure;
		return failure;
	}

	private static IReadOnlyList<string> BuildWalletAddressCandidates(string walletAddress)
	{
		var trimmed = walletAddress.Trim();
		var candidates = new List<string>();

		if (trimmed.Length == 0)
			return candidates;

		candidates.Add(trimmed);

		try
		{
			var normalized = new Address(trimmed).ToString();
			if (!string.Equals(normalized, trimmed, StringComparison.Ordinal))
				candidates.Add(normalized);
		}
		catch
		{
			// Keep the original address candidate if normalization fails.
		}

		return candidates;
	}

	private static IEnumerable<bool> GetArchivalModes()
	{
		yield return false;
		yield return true;
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
			var cell = Cell.From(body.Trim());
			var slice = cell.Parse();
			var op = slice.LoadUInt(32);
			if (op != 0)
				return null;

			var comment = slice.LoadString().TrimEnd('\0');
			return string.IsNullOrWhiteSpace(comment) ? null : comment;
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

	private static decimal ToNanotons(decimal tonAmount)
		=> decimal.Round(tonAmount * 1_000_000_000m, 0, MidpointRounding.AwayFromZero);

	private sealed record TelegramTonTransactionCandidate(
		string? TransactionId,
		DateTimeOffset? ObservedAtUtc,
		string? Memo,
		decimal? ValueNanotons,
		string? SenderAddress);

	private sealed record CachedTransactionBatch(
		bool Success,
		IReadOnlyList<TelegramTonTransactionCandidate> Transactions,
		string? Error,
		int RawTransactionCount,
		DateTimeOffset ExpiresAtUtc);
}



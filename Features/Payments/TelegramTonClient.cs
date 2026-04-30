using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TonSdk.Client;
using TonSdk.Core;
using TonSdk.Core.Boc;

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

		try
		{
			using var client = CreateTonClient(baseUrl);
			var address = new Address(walletAddress.Trim());
			var response = await client.GetTransactions(
				address,
				checked((uint)Math.Clamp(limit, 1, 100)),
				null,
				null,
				null,
				archival);

			var rawTransactions = response ?? [];
			var transactions = new List<TelegramTonTransactionCandidate>();
			foreach (var tx in rawTransactions)
			{
				transactions.Add(new TelegramTonTransactionCandidate(
					string.IsNullOrWhiteSpace(tx.TransactionId.Hash) ? null : tx.TransactionId.Hash,
					DateTimeOffset.FromUnixTimeSeconds(tx.UTime),
					ExtractMessageText(tx.InMsg.Message),
					ToNanotons(tx.InMsg.Value.ToDecimal()),
					tx.InMsg.Source?.ToString()));
			}

			var success = new CachedTransactionBatch(true, transactions, null, rawTransactions.Length, now.AddSeconds(5));
			TransactionCache[cacheKey] = success;
			return success;
		}
		catch (Exception ex)
		{
			return CacheFailure(cacheKey, now, ex.Message);
		}
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

	private static string? ExtractMessageText(string? message)
	{
		var normalized = (message ?? string.Empty).Trim().TrimEnd('\0');
		return normalized.Length == 0 ? null : normalized;
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

	private TonClient CreateTonClient(string baseUrl)
		=> new(
			TonClientType.HTTP_TONCENTERAPIV2,
			new HttpParameters
			{
				Endpoint = baseUrl,
				ApiKey = string.IsNullOrWhiteSpace(_options.TelegramTon.ApiKey) ? null : _options.TelegramTon.ApiKey.Trim(),
				Timeout = _options.TelegramTon.RequestTimeoutSeconds <= 0 ? 15 : _options.TelegramTon.RequestTimeoutSeconds
			});

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



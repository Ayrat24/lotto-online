namespace MiniApp.Features.Payments;

public interface ITelegramTonClient
{
    Task<TelegramTonLookupResult> TryFindIncomingTransferAsync(TelegramTonLookupRequest request, CancellationToken ct);

    Task<TelegramTonUsdRateResult> GetUsdPerTonRateAsync(CancellationToken ct);
}

public sealed record TelegramTonLookupRequest(
    string WalletAddress,
    string ReferenceMemo,
    decimal ExpectedTonAmount,
    DateTimeOffset CreatedAfterUtc,
    int SearchLimit,
    string? ExplorerBaseUrl = null);

public sealed record TelegramTonLookupResult(
    bool Success,
    bool TransferFound,
    string? Error = null,
    string? TransactionId = null,
    decimal? ReceivedTonAmount = null,
    DateTimeOffset? ObservedAtUtc = null,
    string? ExplorerLink = null,
    string? SenderAddress = null);

public sealed record TelegramTonUsdRateQuote(
    decimal UsdPerTon,
    DateTimeOffset ObservedAtUtc,
    string Source);

public sealed record TelegramTonUsdRateResult(
    bool Success,
    string? Error = null,
    TelegramTonUsdRateQuote? Quote = null);



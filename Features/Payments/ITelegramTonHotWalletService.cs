namespace MiniApp.Features.Payments;

public interface ITelegramTonHotWalletService
{
    Task<TelegramTonHotWalletStateResult> GetHotWalletStateAsync(CancellationToken ct);

    Task<TelegramTonSendWithdrawalResult> SendWithdrawalAsync(TelegramTonSendWithdrawalRequest request, CancellationToken ct);

    Task<TelegramTonOutgoingTransferLookupResult> TryFindOutgoingTransferAsync(TelegramTonOutgoingTransferLookupRequest request, CancellationToken ct);
}

public sealed record TelegramTonSendWithdrawalRequest(
    string DestinationAddress,
    decimal AmountTon,
    string Memo,
    int Seqno,
    bool Bounce,
    int ValidForSeconds);

public sealed record TelegramTonHotWalletStateResult(
    bool Success,
    string? Error = null,
    string? Address = null,
    decimal? BalanceTon = null,
    int? Seqno = null,
    bool IsDeployed = false,
    string? DerivedAddress = null,
    string? ExpectedAddress = null,
    int? Workchain = null,
    int? Revision = null,
    int? SubwalletId = null,
    string? WalletVersion = null,
    int? NetworkGlobalId = null,
    bool CanSignTransferProbe = false,
    string? TransferProbeError = null);

public sealed record TelegramTonSendWithdrawalResult(
    bool Success,
    string? Error = null,
    string? ExternalMessageHash = null,
    int? Seqno = null,
    DateTimeOffset? SubmittedAtUtc = null);

public sealed record TelegramTonOutgoingTransferLookupRequest(
    string DestinationAddress,
    decimal AmountTon,
    string Memo,
    DateTimeOffset CreatedAfterUtc,
    int SearchLimit);

public sealed record TelegramTonOutgoingTransferLookupResult(
    bool Success,
    bool TransferFound,
    string? Error = null,
    string? TransactionHash = null,
    DateTimeOffset? ObservedAtUtc = null,
    decimal? AmountTon = null);



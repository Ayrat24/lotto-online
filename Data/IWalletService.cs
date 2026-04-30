namespace MiniApp.Data;

public interface IWalletService
{
    decimal TopUpAmount { get; }

    Task<decimal> TopUpUserAsync(long userId, CancellationToken ct);

    Task<WalletBatchPurchaseResult> TryPurchaseTicketsAsync(long userId, long drawId, IReadOnlyList<string> numbersByTicket, long? offerId, CancellationToken ct);

    Task<WalletClaimResult> ClaimTicketWinningsAsync(long userId, long ticketId, CancellationToken ct);

    Task<WalletWithdrawRequestResult> CreateWithdrawalRequestAsync(long userId, decimal amount, string assetCode, string? address, bool saveAddress, CancellationToken ct);

    Task<WalletSaveAddressResult> SaveWalletAddressAsync(long userId, string assetCode, string address, CancellationToken ct);

    Task<WalletSavedAddresses> GetWalletAddressesAsync(long userId, CancellationToken ct);

    Task<IReadOnlyList<WalletHistoryEntry>> GetHistoryAsync(long userId, int limit, CancellationToken ct);

    Task<WalletReviewWithdrawalResult> ConfirmWithdrawalAsync(long withdrawalRequestId, string adminUsername, CancellationToken ct);

    Task<WalletReviewWithdrawalResult> DenyWithdrawalAsync(long withdrawalRequestId, string adminUsername, string? note, CancellationToken ct);

    Task<ServerWallet> EnsureServerWalletAsync(CancellationToken ct);
}

public sealed record WalletPurchaseResult(bool Success, decimal UserBalance, string? Error, Ticket? Ticket = null);
public sealed record WalletBatchPurchaseResult(bool Success, decimal UserBalance, decimal TotalCost, string? Error, IReadOnlyList<Ticket>? Tickets = null);
public sealed record WalletClaimResult(bool Success, decimal UserBalance, decimal Amount, string? Error);

public sealed record WalletSavedAddresses(string? BitcoinAddress, string? TonAddress);

public sealed record WalletWithdrawRequestResult(bool Success, decimal UserBalance, string? Error, WithdrawalRequest? Request = null, WalletSavedAddresses? SavedAddresses = null);

public sealed record WalletReviewWithdrawalResult(bool Success, string? Error);

public sealed record WalletSaveAddressResult(bool Success, string? Error, WalletSavedAddresses? SavedAddresses = null, string? SavedAddress = null);

public sealed record WalletHistoryEntry(
    string Kind,
    string Status,
    decimal Amount,
    string Currency,
    DateTimeOffset CreatedAtUtc,
    string? ExternalId = null,
    string? Note = null,
    string? PaymentMethod = null,
    string? AssetCode = null,
    decimal? AssetAmount = null);



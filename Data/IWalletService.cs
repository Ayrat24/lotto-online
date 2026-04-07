namespace MiniApp.Data;

public interface IWalletService
{
    decimal TopUpAmount { get; }

    Task<decimal> TopUpUserAsync(long userId, CancellationToken ct);

    Task<WalletPurchaseResult> TryPurchaseTicketAsync(long userId, long drawId, string numbers, CancellationToken ct);

    Task<WalletClaimResult> ClaimTicketWinningsAsync(long userId, long ticketId, CancellationToken ct);

    Task<WalletWithdrawRequestResult> CreateWithdrawalRequestAsync(long userId, decimal amount, string number, CancellationToken ct);

    Task<WalletReviewWithdrawalResult> ConfirmWithdrawalAsync(long withdrawalRequestId, string adminUsername, CancellationToken ct);

    Task<WalletReviewWithdrawalResult> DenyWithdrawalAsync(long withdrawalRequestId, string adminUsername, string? note, CancellationToken ct);

    Task<ServerWallet> EnsureServerWalletAsync(CancellationToken ct);
}

public sealed record WalletPurchaseResult(bool Success, decimal UserBalance, string? Error, Ticket? Ticket = null);
public sealed record WalletClaimResult(bool Success, decimal UserBalance, decimal Amount, string? Error);

public sealed record WalletWithdrawRequestResult(bool Success, decimal UserBalance, string? Error, WithdrawalRequest? Request = null);

public sealed record WalletReviewWithdrawalResult(bool Success, string? Error);



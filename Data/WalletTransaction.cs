namespace MiniApp.Data;

public enum WalletTransactionType
{
    TopUp = 0,
    TicketPurchase = 1,
    WithdrawalRequested = 2,
    WithdrawalConfirmed = 3,
    WithdrawalDeniedRefund = 4,
    WinningsClaimed = 5,
    CryptoDepositCredited = 6,
    ReferralInviterBonus = 7,
    ReferralInviteeBonus = 8
}

public sealed class WalletTransaction
{
    public long Id { get; set; }

    public long UserId { get; set; }
    public MiniAppUser User { get; set; } = null!;

    public WalletTransactionType Type { get; set; }

    public decimal UserDelta { get; set; }

    public decimal UserBalanceAfter { get; set; }

    public decimal ServerDelta { get; set; }

    public decimal ServerBalanceAfter { get; set; }

    public string? Reference { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

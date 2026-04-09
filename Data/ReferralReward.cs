namespace MiniApp.Data;

public enum ReferralRewardType
{
    InviterBonus = 0,
    InviteeBonus = 1
}

public sealed class ReferralReward
{
    public long Id { get; set; }

    public long InviterUserId { get; set; }
    public MiniAppUser InviterUser { get; set; } = null!;

    public long InviteeUserId { get; set; }
    public MiniAppUser InviteeUser { get; set; } = null!;

    public long RecipientUserId { get; set; }
    public MiniAppUser RecipientUser { get; set; } = null!;

    public long DepositIntentId { get; set; }
    public CryptoDepositIntent DepositIntent { get; set; } = null!;

    public ReferralRewardType Type { get; set; }

    public decimal Amount { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}


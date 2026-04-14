namespace MiniApp.Data;

public sealed class MiniAppUser
{
    public const long UnboundReferralUserId = -1;

    public long Id { get; set; }

    /// <summary>
    /// Telegram user id (from Update.Message.From.Id)
    /// </summary>
    public long TelegramUserId { get; set; }

    /// <summary>
    /// User-submitted number collected via Telegram bot onboarding.
    /// </summary>
    public string? Number { get; set; }

    /// <summary>
    /// UI locale selected by the user (en, ru, uz).
    /// </summary>
    public string? PreferredLanguage { get; set; }

    public string? WalletAddress { get; set; }

    public string? InviteCode { get; set; }

    /// <summary>
    /// Referring user id, or -1 when the account is not bound.
    /// </summary>
    public long ReferredByUserId { get; set; } = UnboundReferralUserId;

    public DateTimeOffset? ReferredAtUtc { get; set; }

    public bool IsFake { get; set; }

    public decimal Balance { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset LastSeenAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

namespace MiniApp.Data;

public static class WithdrawalAssetCodes
{
    public const string Bitcoin = "BTC";
    public const string Ton = "TON";

    public static string? Normalize(string? value, bool defaultToBitcoin = false)
    {
        var normalized = (value ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
            return defaultToBitcoin ? Bitcoin : null;

        return normalized switch
        {
            Bitcoin or "BITCOIN" => Bitcoin,
            Ton => Ton,
            _ => null
        };
    }
}

public enum WithdrawalRequestStatus
{
    Pending = 0,
    Confirmed = 1,
    Denied = 2
}

public static class TonWithdrawalPayoutStates
{
    public const string Queued = "queued";
    public const string Sending = "sending";
    public const string Submitted = "submitted";
    public const string Confirmed = "confirmed";
    public const string RetryPending = "retry_pending";
    public const string Failed = "failed";
}

public sealed class WithdrawalRequest
{
    public long Id { get; set; }

    public long UserId { get; set; }
    public MiniAppUser User { get; set; } = null!;

    public decimal Amount { get; set; }

    public string AssetCode { get; set; } = WithdrawalAssetCodes.Bitcoin;

    public decimal? AssetAmount { get; set; }

    public decimal? AssetRate { get; set; }

    public string Number { get; set; } = null!;

    public string? ExternalPayoutId { get; set; }

    public string? ExternalPayoutState { get; set; }

    public DateTimeOffset? ExternalPayoutCreatedAtUtc { get; set; }

    public string? PayoutMemo { get; set; }

    public int PayoutAttemptCount { get; set; }

    public int? PayoutSeqno { get; set; }

    public DateTimeOffset? PayoutLastAttemptAtUtc { get; set; }

    public DateTimeOffset? PayoutSubmittedAtUtc { get; set; }

    public DateTimeOffset? PayoutConfirmedAtUtc { get; set; }

    public string? PayoutLastError { get; set; }

    public WithdrawalRequestStatus Status { get; set; } = WithdrawalRequestStatus.Pending;

    public string? ReviewedByAdmin { get; set; }

    public string? ReviewNote { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ReviewedAtUtc { get; set; }
}


namespace MiniApp.Data;

public enum WithdrawalRequestStatus
{
    Pending = 0,
    Confirmed = 1,
    Denied = 2
}

public sealed class WithdrawalRequest
{
    public long Id { get; set; }

    public long UserId { get; set; }
    public MiniAppUser User { get; set; } = null!;

    public decimal Amount { get; set; }

    public string Number { get; set; } = null!;

    public WithdrawalRequestStatus Status { get; set; } = WithdrawalRequestStatus.Pending;

    public string? ReviewedByAdmin { get; set; }

    public string? ReviewNote { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ReviewedAtUtc { get; set; }
}


namespace MiniApp.Data;

public enum TicketStatus
{
    AwaitingDraw = 0,
    ExpiredNoWin = 1,
    WinningsAvailable = 2,
    WinningsClaimed = 3
}

public sealed class Ticket
{
    public long Id { get; set; }

    public long UserId { get; set; }
    public MiniAppUser User { get; set; } = null!;

    public long DrawId { get; set; }
    public Draw Draw { get; set; } = null!;

    /// <summary>
    /// 5 distinct numbers in range 1..36 (sorted), stored as "n1,n2,n3,n4,n5".
    /// </summary>
    public string Numbers { get; set; } = null!;

    public string? NumbersSignature { get; set; }

    public TicketStatus Status { get; set; } = TicketStatus.AwaitingDraw;

    /// <summary>
    /// Prize locked in for this ticket when its draw was executed. For winning tickets this is
    /// the ticket's share of the tier prize pool (pool split across all winners of that tier);
    /// 0 for non-winning or not-yet-drawn tickets. Persisted so the payout is fixed at draw time
    /// and read consistently by claim and display paths.
    /// </summary>
    public decimal WinningAmount { get; set; }

    public DateTimeOffset PurchasedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

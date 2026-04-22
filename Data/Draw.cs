namespace MiniApp.Data;

public enum DrawState
{
    Upcoming = 0,
    Active = 1,
    Finished = 2
}

public sealed class Draw
{
    public long Id { get; set; }

    public decimal PrizePoolMatch3 { get; set; }

    public decimal PrizePoolMatch4 { get; set; }

    public decimal PrizePoolMatch5 { get; set; }

    public decimal TicketCost { get; set; } = 2m;

    public DrawState State { get; set; } = DrawState.Upcoming;

    public DateTimeOffset PurchaseClosesAtUtc { get; set; } = DateTimeOffset.UtcNow.AddHours(1);

    /// <summary>
    /// 5 distinct numbers in range 1..36 (sorted), stored as "n1,n2,n3,n4,n5".
    /// </summary>
    public string? Numbers { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

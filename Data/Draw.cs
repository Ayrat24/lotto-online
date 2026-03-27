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

    public decimal PrizePool { get; set; }

    public DrawState State { get; set; } = DrawState.Upcoming;

    /// <summary>
    /// 6 distinct numbers in range 1..49 (sorted), stored as "n1,n2,n3,n4,n5,n6".
    /// </summary>
    public string? Numbers { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

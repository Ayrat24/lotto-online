namespace MiniApp.Data;

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

    public DateTimeOffset PurchasedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

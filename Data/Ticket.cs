namespace MiniApp.Data;

public sealed class Ticket
{
    public long Id { get; set; }

    public long UserId { get; set; }
    public MiniAppUser User { get; set; } = null!;

    public long DrawId { get; set; }

    /// <summary>
    /// 6 distinct numbers in range 1..49 (sorted), stored as "n1,n2,n3,n4,n5,n6".
    /// </summary>
    public string Numbers { get; set; } = null!;

    public DateTimeOffset PurchasedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

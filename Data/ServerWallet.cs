namespace MiniApp.Data;

public sealed class ServerWallet
{
    // Single row aggregate wallet (Id=1).
    public int Id { get; set; } = 1;

    public decimal Balance { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}


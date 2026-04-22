namespace MiniApp.Data;

public sealed class TicketPurchaseSettings
{
    public int Id { get; set; } = 1;

    public int TicketSlotsCount { get; set; } = 10;

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public string? UpdatedByAdmin { get; set; }
}


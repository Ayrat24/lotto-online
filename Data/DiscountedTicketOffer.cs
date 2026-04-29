namespace MiniApp.Data;

public sealed class DiscountedTicketOffer
{
    public long Id { get; set; }

    public long DrawId { get; set; }

    public Draw Draw { get; set; } = null!;

    public int NumberOfDiscountedTickets { get; set; }

    public decimal Cost { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}


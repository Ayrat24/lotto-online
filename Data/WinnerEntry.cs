namespace MiniApp.Data;

public sealed class WinnerEntry
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string WinningAmountText { get; set; } = string.Empty;
    public string? QuoteText { get; set; }
    public string PhotoPath { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsPublished { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}



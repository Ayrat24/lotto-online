namespace MiniApp.Data;

public sealed class NewsBanner
{
    public long Id { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string? ActionValue { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPublished { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}


namespace MiniApp.Data;

public sealed class Promotion
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string ButtonText { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string? ActionValue { get; set; }
    public string BackgroundColor { get; set; } = "#FFB929";
    public int DisplayOrder { get; set; }
    public bool IsPublished { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

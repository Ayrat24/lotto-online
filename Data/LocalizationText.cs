namespace MiniApp.Data;

public sealed class LocalizationText
{
    public long Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public string EnglishValue { get; set; } = string.Empty;

    public string RussianValue { get; set; } = string.Empty;

    public string UzbekValue { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}


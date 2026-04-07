namespace MiniApp.Data;

public sealed class MiniAppUser
{
    public long Id { get; set; }

    /// <summary>
    /// Telegram user id (from Update.Message.From.Id)
    /// </summary>
    public long TelegramUserId { get; set; }

    /// <summary>
    /// User-submitted number collected via Telegram bot onboarding.
    /// </summary>
    public string? Number { get; set; }

    public decimal Balance { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset LastSeenAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

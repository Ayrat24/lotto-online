namespace MiniApp.Data;

public sealed class MiniAppUser
{
    public long Id { get; set; }

    /// <summary>
    /// Telegram user id (from Update.Message.From.Id)
    /// </summary>
    public long TelegramUserId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset LastSeenAtUtc { get; set; } = DateTimeOffset.UtcNow;
}


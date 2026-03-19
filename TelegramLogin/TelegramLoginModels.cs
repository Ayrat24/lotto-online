namespace MiniApp.TelegramLogin;

public sealed record TelegramAuthRequest(string InitData);

public sealed record TelegramAuthResult(
    bool Ok,
    long? TelegramUserId,
    string? Username,
    string? FirstName,
    string? LastName,
    string? Error);


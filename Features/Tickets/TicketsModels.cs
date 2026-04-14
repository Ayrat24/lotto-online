namespace MiniApp.Features.Tickets;

public sealed record InitDataRequest(string InitData);

public sealed record PurchaseTicketRequest(string InitData, IReadOnlyList<int>? Numbers, long DrawId);

public sealed record ClaimTicketRequest(string InitData, long TicketId);

public sealed record TicketDto(long Id, long DrawId, string Numbers, string Status, DateTimeOffset PurchasedAtUtc, decimal WinningAmount);

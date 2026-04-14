namespace MiniApp.Features.Tickets;

public sealed record PurchaseTicketRequest(string InitData, IReadOnlyList<int>? Numbers, long? DrawId = null);

public sealed record ClaimTicketRequest(string InitData, long TicketId);

public sealed record TicketDto(long Id, long DrawId, string Numbers, string Status, DateTimeOffset PurchasedAtUtc, decimal WinningAmount);

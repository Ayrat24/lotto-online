namespace MiniApp.Features.Tickets;

public sealed record InitDataRequest(string InitData);

public sealed record PurchaseTicketsRequest(string InitData, long DrawId, IReadOnlyList<IReadOnlyList<int>>? Tickets);

public sealed record ClaimTicketRequest(string InitData, long TicketId);

public sealed record TicketDto(long Id, long DrawId, string Numbers, string Status, DateTimeOffset PurchasedAtUtc, decimal WinningAmount);

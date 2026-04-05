namespace MiniApp.Features.Tickets;

public sealed record PurchaseTicketRequest(string InitData, IReadOnlyList<int>? Numbers);

public sealed record TicketDto(long Id, long DrawId, string Numbers, DateTimeOffset PurchasedAtUtc);

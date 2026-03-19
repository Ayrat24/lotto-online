namespace MiniApp.Features.Tickets;

public sealed record PurchaseTicketRequest(string InitData);

public sealed record TicketDto(long Id, string Numbers, DateTimeOffset PurchasedAtUtc);


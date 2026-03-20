namespace MiniApp.Features.Draws;

public sealed record DrawDto(long Id, string Numbers, DateTimeOffset CreatedAtUtc);

public sealed record TicketForDrawDto(long Id, long DrawId, string Numbers, DateTimeOffset PurchasedAtUtc);

public sealed record TimelineItemDto(string Type, DrawDto? Draw, TicketForDrawDto? Ticket);


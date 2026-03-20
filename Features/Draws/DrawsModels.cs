namespace MiniApp.Features.Draws;

public sealed record DrawDto(long Id, string Numbers, DateTimeOffset CreatedAtUtc);

public sealed record TicketForDrawDto(long Id, long DrawId, string Numbers, DateTimeOffset PurchasedAtUtc);

public sealed record DrawGroupDto(long DrawId, DrawDto? Draw, IReadOnlyList<TicketForDrawDto> Tickets);

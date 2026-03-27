namespace MiniApp.Features.Draws;

public sealed record DrawDto(long Id, decimal PrizePool, string State, string? Numbers, DateTimeOffset CreatedAtUtc);

public sealed record CreateDrawRequest(decimal PrizePool);

public sealed record UpdateDrawRequest(decimal PrizePool, string State);

public sealed record TicketForDrawDto(long Id, long DrawId, string Numbers, DateTimeOffset PurchasedAtUtc);

public sealed record DrawGroupDto(long DrawId, DrawDto? Draw, IReadOnlyList<TicketForDrawDto> Tickets);

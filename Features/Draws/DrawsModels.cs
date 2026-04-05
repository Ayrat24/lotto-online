namespace MiniApp.Features.Draws;

public sealed record DrawDto(
	long Id,
	decimal PrizePool,
	decimal PrizePoolMatch3,
	decimal PrizePoolMatch4,
	decimal PrizePoolMatch5,
	string State,
	string? Numbers,
	DateTimeOffset CreatedAtUtc);

public sealed record CreateDrawRequest(decimal PrizePoolMatch3, decimal PrizePoolMatch4, decimal PrizePoolMatch5);

public sealed record UpdateDrawRequest(decimal PrizePoolMatch3, decimal PrizePoolMatch4, decimal PrizePoolMatch5, string State);

public sealed record TicketForDrawDto(long Id, long DrawId, string Numbers, DateTimeOffset PurchasedAtUtc);

public sealed record DrawGroupDto(long DrawId, DrawDto? Draw, IReadOnlyList<TicketForDrawDto> Tickets);

using MiniApp.Data;

namespace MiniApp.Features.Draws;

public sealed record DrawDto(
	long Id,
	decimal PrizePool,
	decimal PrizePoolMatch3,
	decimal PrizePoolMatch4,
	decimal PrizePoolMatch5,
	decimal TicketCost,
	string State,
	string? Numbers,
	DateTimeOffset CreatedAtUtc);

public sealed record CreateDrawRequest(decimal PrizePoolMatch3, decimal PrizePoolMatch4, decimal PrizePoolMatch5, decimal TicketCost);

public sealed record UpdateDrawRequest(decimal PrizePoolMatch3, decimal PrizePoolMatch4, decimal PrizePoolMatch5, decimal TicketCost, string State);

public sealed record TicketForDrawDto(long Id, long DrawId, string Numbers, string Status, DateTimeOffset PurchasedAtUtc, decimal WinningAmount);

public sealed record DrawGroupDto(long DrawId, DrawDto? Draw, IReadOnlyList<TicketForDrawDto> Tickets);

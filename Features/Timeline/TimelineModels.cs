using MiniApp.Features.Draws;

namespace MiniApp.Features.Timeline;

public sealed record TimelineRequest(string InitData);

public sealed record MiniAppStateDto(
    decimal Balance,
    DateTimeOffset ServerNowUtc,
    DrawDto? CurrentDraw,
    IReadOnlyList<DrawDto> ActiveDraws,
    IReadOnlyList<DrawGroupDto> ActiveTicketGroups,
    IReadOnlyList<TicketForDrawDto> CurrentTickets,
    IReadOnlyList<DrawGroupDto> History,
    TicketPurchaseConfigDto TicketPurchase);

public sealed record TicketPurchaseConfigDto(
    int TicketSlotsCount,
    int NumbersPerTicket,
    int MinNumber,
    int MaxNumber);


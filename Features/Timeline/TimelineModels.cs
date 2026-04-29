using MiniApp.Features.Draws;
using MiniApp.Features.Offers;

namespace MiniApp.Features.Timeline;

public sealed record TimelineRequest(string InitData);

public sealed record MiniAppStateDto(
    decimal Balance,
    DateTimeOffset ServerNowUtc,
    DrawDto? CurrentDraw,
    IReadOnlyList<DrawDto> ActiveDraws,
    IReadOnlyList<DiscountedTicketOfferDto> ActiveOffers,
    IReadOnlyList<DrawGroupDto> ActiveTicketGroups,
    IReadOnlyList<TicketForDrawDto> CurrentTickets,
    IReadOnlyList<DrawGroupDto> History,
    TicketPurchaseConfigDto TicketPurchase);

public sealed record TicketPurchaseConfigDto(
    int TicketSlotsCount,
    int NumbersPerTicket,
    int MinNumber,
    int MaxNumber);


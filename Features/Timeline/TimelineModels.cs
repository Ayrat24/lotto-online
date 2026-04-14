using MiniApp.Features.Draws;

namespace MiniApp.Features.Timeline;

public sealed record TimelineRequest(string InitData);

public sealed record MiniAppStateDto(
    decimal Balance,
    DrawDto? CurrentDraw,
    IReadOnlyList<DrawDto> ActiveDraws,
    IReadOnlyList<DrawGroupDto> ActiveTicketGroups,
    IReadOnlyList<TicketForDrawDto> CurrentTickets,
    IReadOnlyList<DrawGroupDto> History);


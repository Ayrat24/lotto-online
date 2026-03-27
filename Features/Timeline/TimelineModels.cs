using MiniApp.Features.Draws;

namespace MiniApp.Features.Timeline;

public sealed record MiniAppStateDto(
    DrawDto? CurrentDraw,
    IReadOnlyList<TicketForDrawDto> CurrentTickets,
    IReadOnlyList<DrawGroupDto> History);


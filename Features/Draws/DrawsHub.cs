using Microsoft.AspNetCore.SignalR;

namespace MiniApp.Features.Draws;

public sealed class DrawsHub : Hub
{
    public const string HubPath = "/hubs/draws";

    // No client->server methods needed right now.
}

using Microsoft.AspNetCore.SignalR;

namespace MiniApp.Features.Draws;

public sealed class DrawsHub : Hub
{
    public const string HubPath = "/hubs/draws";

    public override async Task OnConnectedAsync()
    {
        // Tell the client to refresh its timeline once the connection is up.
        // This avoids relying on polling and ensures the UI is in-sync after reconnects.
        await Clients.Caller.SendAsync("draws_connected");
        await base.OnConnectedAsync();
    }

    // No client->server methods needed right now.
}

using Microsoft.AspNetCore.SignalR;

namespace Sayra.Server.Realtime.Hubs;

public class AdminHub : Hub
{
    public async Task JoinAdminGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
    }

    // Server -> Admin Dashboard Push Events
    // (Note: Methods called by RealtimeEventHandler via IHubContext)
}

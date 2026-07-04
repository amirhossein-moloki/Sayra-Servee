using Microsoft.AspNetCore.SignalR;

namespace Sayra.Server.AdminAPI.Hubs;

public class AdminHub : Hub
{
    public async Task JoinAdminGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
    }

    public async Task NotifyClientStatusChanged(string pcId, string status)
    {
        await Clients.Group("Admins").SendAsync("OnClientStatusChanged", pcId, status);
    }
}

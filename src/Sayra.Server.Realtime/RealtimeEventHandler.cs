using Microsoft.AspNetCore.SignalR;
using Sayra.Server.EventBus.Events;
using Sayra.Server.EventBus.Interfaces;
using Sayra.Server.Realtime.Hubs;

namespace Sayra.Server.Realtime;

public class RealtimeEventHandler
{
    private readonly IHubContext<AdminHub> _hubContext;

    public RealtimeEventHandler(IEventSubscriber subscriber, IHubContext<AdminHub> hubContext)
    {
        _hubContext = hubContext;

        subscriber.Subscribe<ClientConnectedEvent>(HandleClientConnected);
        subscriber.Subscribe<ClientDisconnectedEvent>(HandleClientDisconnected);
        subscriber.Subscribe<ClientAuthenticatedEvent>(HandleClientAuthenticated);
        subscriber.Subscribe<SessionStartedEvent>(HandleSessionStarted);
        subscriber.Subscribe<SessionUpdatedEvent>(HandleSessionUpdated);
        subscriber.Subscribe<SessionEndedEvent>(HandleSessionEnded);
        subscriber.Subscribe<TelemetryReceivedEvent>(HandleTelemetry);
        subscriber.Subscribe<CommandExecutedEvent>(HandleCommand);
    }

    private async Task HandleClientConnected(ClientConnectedEvent @event, CancellationToken ct)
    {
        await _hubContext.Clients.Group("Admins").SendAsync("OnClientConnected", @event, ct);
    }

    private async Task HandleClientDisconnected(ClientDisconnectedEvent @event, CancellationToken ct)
    {
        await _hubContext.Clients.Group("Admins").SendAsync("OnClientDisconnected", @event, ct);
    }

    private async Task HandleClientAuthenticated(ClientAuthenticatedEvent @event, CancellationToken ct)
    {
        await _hubContext.Clients.Group("Admins").SendAsync("OnClientAuthenticated", @event, ct);
    }

    private async Task HandleSessionStarted(SessionStartedEvent @event, CancellationToken ct)
    {
        await _hubContext.Clients.Group("Admins").SendAsync("OnSessionStarted", @event, ct);
    }

    private async Task HandleSessionUpdated(SessionUpdatedEvent @event, CancellationToken ct)
    {
        await _hubContext.Clients.Group("Admins").SendAsync("OnSessionUpdated", @event, ct);
    }

    private async Task HandleSessionEnded(SessionEndedEvent @event, CancellationToken ct)
    {
        await _hubContext.Clients.Group("Admins").SendAsync("OnSessionEnded", @event, ct);
    }

    private async Task HandleTelemetry(TelemetryReceivedEvent @event, CancellationToken ct)
    {
        await _hubContext.Clients.Group("Admins").SendAsync("OnTelemetryReceived", @event, ct);
    }

    private async Task HandleCommand(CommandExecutedEvent @event, CancellationToken ct)
    {
        bool success = !string.IsNullOrEmpty(@event.Result) && !@event.Result.Contains("Error", StringComparison.OrdinalIgnoreCase);
        if (success)
            await _hubContext.Clients.Group("Admins").SendAsync("OnCommandExecuted", @event, ct);
        else
            await _hubContext.Clients.Group("Admins").SendAsync("OnCommandFailed", @event, ct);
    }
}

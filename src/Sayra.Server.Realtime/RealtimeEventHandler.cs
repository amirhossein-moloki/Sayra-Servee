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

        // Subscriptions to contract events for real-time broadcasts
        subscriber.Subscribe<AuthenticationStartedEvent>(HandleAuthenticationStarted);
        subscriber.Subscribe<AuthenticationSucceededEvent>(HandleAuthenticationSucceeded);
        subscriber.Subscribe<AuthenticationFailedEvent>(HandleAuthenticationFailed);
        subscriber.Subscribe<LogoutStartedEvent>(HandleLogoutStarted);
        subscriber.Subscribe<LogoutCompletedEvent>(HandleLogoutCompleted);
        subscriber.Subscribe<SessionExpiredEvent>(HandleSessionExpired);
        subscriber.Subscribe<GameLaunchingEvent>(HandleGameLaunching);
        subscriber.Subscribe<GameStartedEvent>(HandleGameStarted);
        subscriber.Subscribe<GameExitedEvent>(HandleGameExited);
        subscriber.Subscribe<GameCrashedEvent>(HandleGameCrashed);
        subscriber.Subscribe<LaunchFailedEvent>(HandleLaunchFailed);
        subscriber.Subscribe<SecurityBreachDetectedEvent>(HandleSecurityBreachDetected);
        subscriber.Subscribe<BillingUpdateEvent>(HandleBillingUpdate);
    }

    private async Task HandleAuthenticationStarted(AuthenticationStartedEvent @event, CancellationToken ct)
    {
        await _hubContext.Clients.Group("Admins").SendAsync("OnAuthenticationStarted", @event, ct);
    }

    private async Task HandleAuthenticationSucceeded(AuthenticationSucceededEvent @event, CancellationToken ct)
    {
        await _hubContext.Clients.Group("Admins").SendAsync("OnAuthenticationSucceeded", @event, ct);
    }

    private async Task HandleAuthenticationFailed(AuthenticationFailedEvent @event, CancellationToken ct)
    {
        await _hubContext.Clients.Group("Admins").SendAsync("OnAuthenticationFailed", @event, ct);
    }

    private async Task HandleLogoutStarted(LogoutStartedEvent @event, CancellationToken ct)
    {
        await _hubContext.Clients.Group("Admins").SendAsync("OnLogoutStarted", @event, ct);
    }

    private async Task HandleLogoutCompleted(LogoutCompletedEvent @event, CancellationToken ct)
    {
        await _hubContext.Clients.Group("Admins").SendAsync("OnLogoutCompleted", @event, ct);
    }

    private async Task HandleSessionExpired(SessionExpiredEvent @event, CancellationToken ct)
    {
        await _hubContext.Clients.Group("Admins").SendAsync("OnSessionExpired", @event, ct);
    }

    private async Task HandleGameLaunching(GameLaunchingEvent @event, CancellationToken ct)
    {
        await _hubContext.Clients.Group("Admins").SendAsync("OnGameLaunching", @event, ct);
    }

    private async Task HandleGameStarted(GameStartedEvent @event, CancellationToken ct)
    {
        await _hubContext.Clients.Group("Admins").SendAsync("OnGameStarted", @event, ct);
    }

    private async Task HandleGameExited(GameExitedEvent @event, CancellationToken ct)
    {
        await _hubContext.Clients.Group("Admins").SendAsync("OnGameExited", @event, ct);
    }

    private async Task HandleGameCrashed(GameCrashedEvent @event, CancellationToken ct)
    {
        await _hubContext.Clients.Group("Admins").SendAsync("OnGameCrashed", @event, ct);
    }

    private async Task HandleLaunchFailed(LaunchFailedEvent @event, CancellationToken ct)
    {
        await _hubContext.Clients.Group("Admins").SendAsync("OnLaunchFailed", @event, ct);
    }

    private async Task HandleSecurityBreachDetected(SecurityBreachDetectedEvent @event, CancellationToken ct)
    {
        await _hubContext.Clients.Group("Admins").SendAsync("OnSecurityBreachDetected", @event, ct);
    }

    private async Task HandleBillingUpdate(BillingUpdateEvent @event, CancellationToken ct)
    {
        await _hubContext.Clients.Group("Admins").SendAsync("OnBillingUpdate", @event, ct);
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

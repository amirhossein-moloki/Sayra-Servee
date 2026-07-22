using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sayra.Server.Application.Interfaces;
using Sayra.Server.EventBus.Events;
using Sayra.Server.EventBus.Interfaces;
using Sayra.Server.Persistence.Entities;
using Sayra.Server.Session;

namespace Sayra.Server.Application.EventHandlers;

public class PersistenceEventHandlers
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISessionManager _sessionManager;
    private readonly ILogger<PersistenceEventHandlers> _logger;

    public PersistenceEventHandlers(
        IEventSubscriber subscriber,
        IServiceScopeFactory scopeFactory,
        ISessionManager sessionManager,
        ILogger<PersistenceEventHandlers> logger)
    {
        _scopeFactory = scopeFactory;
        _sessionManager = sessionManager;
        _logger = logger;

        subscriber.Subscribe<ClientConnectedEvent>(HandleClientConnectedAsync);
        subscriber.Subscribe<ClientAuthenticatedEvent>(HandleClientAuthenticatedAsync);
        subscriber.Subscribe<SessionStartedEvent>(HandleSessionStartedAsync);
        subscriber.Subscribe<SessionEndedEvent>(HandleSessionEndedAsync);
        subscriber.Subscribe<CommandExecutedEvent>(HandleCommandExecutedAsync);
        subscriber.Subscribe<TelemetryReceivedEvent>(HandleTelemetryReceivedAsync);

        // New contract events subscriptions
        subscriber.Subscribe<AuthenticationStartedEvent>(HandleAuthenticationStartedAsync);
        subscriber.Subscribe<AuthenticationSucceededEvent>(HandleAuthenticationSucceededAsync);
        subscriber.Subscribe<AuthenticationFailedEvent>(HandleAuthenticationFailedAsync);
        subscriber.Subscribe<LogoutStartedEvent>(HandleLogoutStartedAsync);
        subscriber.Subscribe<LogoutCompletedEvent>(HandleLogoutCompletedAsync);
        subscriber.Subscribe<SessionExpiredEvent>(HandleSessionExpiredAsync);
        subscriber.Subscribe<GameLaunchingEvent>(HandleGameLaunchingAsync);
        subscriber.Subscribe<GameStartedEvent>(HandleGameStartedAsync);
        subscriber.Subscribe<GameExitedEvent>(HandleGameExitedAsync);
        subscriber.Subscribe<GameCrashedEvent>(HandleGameCrashedAsync);
        subscriber.Subscribe<LaunchFailedEvent>(HandleLaunchFailedAsync);
    }

    private async Task HandleClientConnectedAsync(ClientConnectedEvent @event, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var clientRepository = scope.ServiceProvider.GetRequiredService<IClientRepository>();

        await clientRepository.UpsertAsync(new ClientEntity
        {
            PcId = @event.ClientId,
            IP = @event.IpAddress,
            Status = "Online",
            LastSeen = DateTime.UtcNow
        });
    }

    private async Task HandleClientAuthenticatedAsync(ClientAuthenticatedEvent @event, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var clientRepository = scope.ServiceProvider.GetRequiredService<IClientRepository>();

        var client = await clientRepository.GetByPcIdAsync(@event.PcId);
        if (client != null)
        {
            client.MacAddress = @event.MacAddress;
            client.LastSeen = DateTime.UtcNow;
            await clientRepository.UpsertAsync(client);
        }
    }

    private async Task HandleSessionStartedAsync(SessionStartedEvent @event, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var sessionRepository = scope.ServiceProvider.GetRequiredService<ISessionRepository>();

        await sessionRepository.AddAsync(new SessionEntity
        {
            SessionId = @event.SessionId,
            PcId = @event.PcId,
            StartTime = @event.OccurredAt,
            Status = "Active"
        });
    }

    private async Task HandleSessionEndedAsync(SessionEndedEvent @event, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var sessionRepository = scope.ServiceProvider.GetRequiredService<ISessionRepository>();

        var session = await sessionRepository.GetByIdAsync(@event.SessionId);
        if (session != null)
        {
            session.EndTime = @event.EndTime;
            session.Status = "Ended";
            session.Duration = (@event.EndTime - session.StartTime).TotalMinutes;
            await sessionRepository.UpdateAsync(session);
        }
    }

    private async Task HandleCommandExecutedAsync(CommandExecutedEvent @event, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var commandRepository = scope.ServiceProvider.GetRequiredService<ICommandRepository>();

        await commandRepository.AddAsync(new CommandAuditEntity
        {
            CommandId = @event.CommandId,
            PcId = @event.PcId,
            Action = @event.Action,
            Result = @event.Result,
            Timestamp = @event.OccurredAt
        });
    }

    private async Task HandleTelemetryReceivedAsync(TelemetryReceivedEvent @event, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var telemetryRepository = scope.ServiceProvider.GetRequiredService<ITelemetryRepository>();

        await telemetryRepository.AddAsync(new TelemetryEntity
        {
            PcId = @event.PcId,
            CPU = @event.CpuUsage,
            RAM = @event.RamUsage,
            Uptime = @event.Uptime,
            Timestamp = @event.OccurredAt
        });
    }

    private Task HandleAuthenticationStartedAsync(AuthenticationStartedEvent @event, CancellationToken ct)
    {
        _logger.LogInformation("[AUDIT] Player authentication started for username: {Username} on client {ClientId}", @event.Username, @event.ClientId);
        return Task.CompletedTask;
    }

    private Task HandleAuthenticationSucceededAsync(AuthenticationSucceededEvent @event, CancellationToken ct)
    {
        _logger.LogInformation("[AUDIT] Player authentication SUCCEEDED for username: {Username} on client {ClientId}. SessionId: {SessionId}", @event.User.Username, @event.ClientId, @event.SessionId);
        return Task.CompletedTask;
    }

    private Task HandleAuthenticationFailedAsync(AuthenticationFailedEvent @event, CancellationToken ct)
    {
        _logger.LogWarning("[AUDIT] Player authentication FAILED for username: {Username} on client {ClientId}. Reason: {Reason}", @event.Username, @event.ClientId, @event.Reason);
        return Task.CompletedTask;
    }

    private Task HandleLogoutStartedAsync(LogoutStartedEvent @event, CancellationToken ct)
    {
        _logger.LogInformation("[AUDIT] Logout started for username: {Username} on client {ClientId}", @event.User.Username, @event.ClientId);

        // End the session as soon as logout starts
        if (_sessionManager.IsSessionActive(@event.ClientId))
        {
            _sessionManager.EndSession(@event.ClientId);
        }
        return Task.CompletedTask;
    }

    private Task HandleLogoutCompletedAsync(LogoutCompletedEvent @event, CancellationToken ct)
    {
        _logger.LogInformation("[AUDIT] Logout completed for username: {Username} on client {ClientId}", @event.Username ?? "Unknown", @event.ClientId);
        return Task.CompletedTask;
    }

    private Task HandleSessionExpiredAsync(SessionExpiredEvent @event, CancellationToken ct)
    {
        _logger.LogWarning("[AUDIT] Session expired for sessionId: {SessionId} on client {ClientId}", @event.SessionId, @event.ClientId);

        // End the session
        if (_sessionManager.IsSessionActive(@event.ClientId))
        {
            _sessionManager.EndSession(@event.ClientId);
        }
        return Task.CompletedTask;
    }

    private Task HandleGameLaunchingAsync(GameLaunchingEvent @event, CancellationToken ct)
    {
        _logger.LogInformation("[AUDIT] Game launching: {GameName} (ID: {GameId}) on client {ClientId}", @event.Name, @event.GameId, @event.ClientId);
        return Task.CompletedTask;
    }

    private Task HandleGameStartedAsync(GameStartedEvent @event, CancellationToken ct)
    {
        _logger.LogInformation("[AUDIT] Game started: {GameName} (ID: {GameId}) with PID {Pid} on client {ClientId}", @event.Name, @event.GameId, @event.Pid, @event.ClientId);
        return Task.CompletedTask;
    }

    private Task HandleGameExitedAsync(GameExitedEvent @event, CancellationToken ct)
    {
        _logger.LogInformation("[AUDIT] Game exited: {GameName} (ID: {GameId}) with exit code {ExitCode} after running for {Duration} on client {ClientId}", @event.Name, @event.GameId, @event.ExitCode, @event.Duration, @event.ClientId);
        return Task.CompletedTask;
    }

    private Task HandleGameCrashedAsync(GameCrashedEvent @event, CancellationToken ct)
    {
        _logger.LogWarning("[AUDIT] Game crashed: {GameName} (ID: {GameId}) with exit code {ExitCode}. Reason: {Reason} on client {ClientId}", @event.Name, @event.GameId, @event.ExitCode, @event.Reason, @event.ClientId);
        return Task.CompletedTask;
    }

    private Task HandleLaunchFailedAsync(LaunchFailedEvent @event, CancellationToken ct)
    {
        _logger.LogWarning("[AUDIT] Game launch FAILED: {GameName} (ID: {GameId}). Reason: {Reason} on client {ClientId}", @event.Name, @event.GameId, @event.Reason, @event.ClientId);
        return Task.CompletedTask;
    }
}

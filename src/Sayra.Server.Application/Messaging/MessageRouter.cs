using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sayra.Server.Application.Interfaces;
using Sayra.Server.Domain.Entities;
using Sayra.Server.Domain.Enums;
using Sayra.Server.EventBus.Events;
using Sayra.Server.EventBus.Interfaces;
using Sayra.Server.Session;
using Sayra.Server.Shared.Messages;

namespace Sayra.Server.Application.Messaging;

public class MessageRouter : IMessageRouter
{
    private readonly ILogger<MessageRouter> _logger;
    private readonly IClientRegistry _clientRegistry;
    private readonly ISessionManager _sessionManager;
    private readonly CommandAuthorizer _authorizer;
    private readonly ISecureMessageDispatcher _dispatcher;
    private readonly IEventPublisher _eventPublisher;

    public MessageRouter(
        ILogger<MessageRouter> logger,
        IClientRegistry clientRegistry,
        ISessionManager sessionManager,
        CommandAuthorizer authorizer,
        ISecureMessageDispatcher dispatcher,
        IEventPublisher eventPublisher)
    {
        _logger = logger;
        _clientRegistry = clientRegistry;
        _sessionManager = sessionManager;
        _authorizer = authorizer;
        _dispatcher = dispatcher;
        _eventPublisher = eventPublisher;
    }

    public Client? GetClient(string clientId) => _clientRegistry.GetById(clientId);

    public void UpdateClient(Client client) => _clientRegistry.AddOrUpdate(client);

    public async Task RouteAsync(string rawMessage)
    {
        try
        {
            var baseMessage = JsonSerializer.Deserialize<BaseMessage>(rawMessage, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (baseMessage == null) return;

            if (!_authorizer.IsAuthorized(baseMessage))
            {
                _logger.LogWarning("Unauthorized message of type {MessageType} from {ClientId}", baseMessage.Type, baseMessage.ClientId);
                return;
            }

            _logger.LogDebug("Routing message of type {MessageType} from {ClientId}", baseMessage.Type, baseMessage.ClientId);

            switch (baseMessage.Type.ToUpper())
            {
                case "HEARTBEAT":
                    HandleHeartbeat(rawMessage);
                    break;
                case "PING":
                    HandlePing(rawMessage);
                    break;
                case "CLIENT_CONNECTED":
                    HandleClientConnected(rawMessage);
                    break;
                case "CLIENT_DISCONNECTED":
                    HandleClientDisconnected(rawMessage);
                    break;
                case "COMMAND":
                    await _dispatcher.DispatchAsync(baseMessage);
                    break;
                case "AUTHENTICATIONSTARTED":
                    await HandleAuthenticationStarted(rawMessage);
                    break;
                case "AUTHENTICATIONSUCCEEDED":
                    await HandleAuthenticationSucceeded(rawMessage);
                    break;
                case "AUTHENTICATIONFAILED":
                    await HandleAuthenticationFailed(rawMessage);
                    break;
                case "LOGOUTSTARTED":
                    await HandleLogoutStarted(rawMessage);
                    break;
                case "LOGOUTCOMPLETED":
                    await HandleLogoutCompleted(rawMessage);
                    break;
                case "SESSIONEXPIRED":
                    await HandleSessionExpired(rawMessage);
                    break;
                case "GAMELAUNCHING":
                    await HandleGameLaunching(rawMessage);
                    break;
                case "GAMESTARTED":
                    await HandleGameStarted(rawMessage);
                    break;
                case "GAMEEXITED":
                    await HandleGameExited(rawMessage);
                    break;
                case "GAMECRASHED":
                    await HandleGameCrashed(rawMessage);
                    break;
                case "LAUNCHFAILED":
                    await HandleLaunchFailed(rawMessage);
                    break;
                default:
                    _logger.LogWarning("Unknown message type: {MessageType}", baseMessage.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing message: {RawMessage}", rawMessage);
        }

        await Task.CompletedTask;
    }

    private async Task HandleAuthenticationStarted(string raw)
    {
        var msg = JsonSerializer.Deserialize<AuthenticationStartedMessage>(raw, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (msg == null) return;

        string ts = msg.Timestamp?.ToString() ?? string.Empty;

        // Validation
        if (string.IsNullOrEmpty(msg.Username))
        {
            _logger.LogWarning("Rejecting AuthenticationStarted: missing username");
            return;
        }
        if (string.IsNullOrEmpty(ts) || !DateTime.TryParse(ts, out _))
        {
            _logger.LogWarning("Rejecting AuthenticationStarted: missing or invalid timestamp");
            return;
        }

        await _eventPublisher.PublishAsync(new AuthenticationStartedEvent(msg.ClientId, msg.Username, ts));
    }

    private async Task HandleAuthenticationSucceeded(string raw)
    {
        var msg = JsonSerializer.Deserialize<AuthenticationSucceededMessage>(raw, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (msg == null) return;

        string ts = msg.Timestamp?.ToString() ?? string.Empty;

        // Validation
        if (msg.User == null)
        {
            _logger.LogWarning("Rejecting AuthenticationSucceeded: missing user");
            return;
        }
        if (string.IsNullOrEmpty(msg.User.Id) || string.IsNullOrEmpty(msg.User.Username) ||
            string.IsNullOrEmpty(msg.User.DisplayName) || string.IsNullOrEmpty(msg.User.Role))
        {
            _logger.LogWarning("Rejecting AuthenticationSucceeded: missing user details");
            return;
        }
        var role = msg.User.Role.ToLower();
        if (role != "player" && role != "admin" && role != "operator")
        {
            _logger.LogWarning("Rejecting AuthenticationSucceeded: invalid user role {Role}", msg.User.Role);
            return;
        }
        var authType = msg.AuthenticationType;
        if (authType != "LocalAdmin" && authType != "ServerReservation" && authType != "Offline" && authType != "Cached" && authType != "Server")
        {
            _logger.LogWarning("Rejecting AuthenticationSucceeded: invalid auth type {Type}", authType);
            return;
        }
        if (string.IsNullOrEmpty(msg.SessionId))
        {
            _logger.LogWarning("Rejecting AuthenticationSucceeded: missing sessionId");
            return;
        }
        if (string.IsNullOrEmpty(ts) || !DateTime.TryParse(ts, out _))
        {
            _logger.LogWarning("Rejecting AuthenticationSucceeded: missing or invalid timestamp");
            return;
        }

        var eventUser = new Sayra.Server.EventBus.Events.EventUserDto
        {
            Id = msg.User.Id,
            Username = msg.User.Username,
            DisplayName = msg.User.DisplayName,
            Role = msg.User.Role,
            Permissions = msg.User.Permissions,
            Avatar = msg.User.Avatar,
            LastLogin = msg.User.LastLogin,
            PreferredLanguage = msg.User.PreferredLanguage,
            PreferredTheme = msg.User.PreferredTheme,
            StationId = msg.User.StationId
        };

        await _eventPublisher.PublishAsync(new AuthenticationSucceededEvent(msg.ClientId, eventUser, msg.AuthenticationType, msg.SessionId, ts));
    }

    private async Task HandleAuthenticationFailed(string raw)
    {
        var msg = JsonSerializer.Deserialize<AuthenticationFailedMessage>(raw, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (msg == null) return;

        string ts = msg.Timestamp?.ToString() ?? string.Empty;

        // Validation
        if (string.IsNullOrEmpty(msg.Username) || string.IsNullOrEmpty(msg.Reason))
        {
            _logger.LogWarning("Rejecting AuthenticationFailed: missing fields");
            return;
        }
        if (string.IsNullOrEmpty(ts) || !DateTime.TryParse(ts, out _))
        {
            _logger.LogWarning("Rejecting AuthenticationFailed: missing or invalid timestamp");
            return;
        }

        await _eventPublisher.PublishAsync(new AuthenticationFailedEvent(msg.ClientId, msg.Username, msg.Reason, ts));
    }

    private async Task HandleLogoutStarted(string raw)
    {
        var msg = JsonSerializer.Deserialize<LogoutStartedMessage>(raw, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (msg == null) return;

        string ts = msg.Timestamp?.ToString() ?? string.Empty;

        // Validation
        if (msg.User == null || string.IsNullOrEmpty(msg.User.Username) || string.IsNullOrEmpty(msg.User.Role))
        {
            _logger.LogWarning("Rejecting LogoutStarted: missing user info");
            return;
        }
        if (string.IsNullOrEmpty(ts) || !DateTime.TryParse(ts, out _))
        {
            _logger.LogWarning("Rejecting LogoutStarted: missing or invalid timestamp");
            return;
        }

        var logoutUser = new Sayra.Server.EventBus.Events.LogoutUserDto
        {
            Username = msg.User.Username,
            Role = msg.User.Role
        };

        await _eventPublisher.PublishAsync(new LogoutStartedEvent(msg.ClientId, logoutUser, ts));
    }

    private async Task HandleLogoutCompleted(string raw)
    {
        var msg = JsonSerializer.Deserialize<LogoutCompletedMessage>(raw, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (msg == null) return;

        string ts = msg.Timestamp?.ToString() ?? string.Empty;

        // Validation
        if (string.IsNullOrEmpty(ts) || !DateTime.TryParse(ts, out _))
        {
            _logger.LogWarning("Rejecting LogoutCompleted: missing or invalid timestamp");
            return;
        }

        await _eventPublisher.PublishAsync(new LogoutCompletedEvent(msg.ClientId, msg.Username, ts));
    }

    private async Task HandleSessionExpired(string raw)
    {
        var msg = JsonSerializer.Deserialize<SessionExpiredMessage>(raw, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (msg == null) return;

        string ts = msg.Timestamp?.ToString() ?? string.Empty;

        // Validation
        if (string.IsNullOrEmpty(msg.SessionId))
        {
            _logger.LogWarning("Rejecting SessionExpired: missing sessionId");
            return;
        }
        if (string.IsNullOrEmpty(ts) || !DateTime.TryParse(ts, out _))
        {
            _logger.LogWarning("Rejecting SessionExpired: missing or invalid timestamp");
            return;
        }

        await _eventPublisher.PublishAsync(new SessionExpiredEvent(msg.ClientId, msg.SessionId, msg.Username, ts));
    }

    private async Task HandleGameLaunching(string raw)
    {
        var msg = JsonSerializer.Deserialize<GameLaunchingMessage>(raw, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (msg == null) return;

        // Validation
        if (string.IsNullOrEmpty(msg.GameId) || string.IsNullOrEmpty(msg.Name))
        {
            _logger.LogWarning("Rejecting GameLaunching: missing game details");
            return;
        }

        await _eventPublisher.PublishAsync(new GameLaunchingEvent(msg.ClientId, msg.GameId, msg.Name));
    }

    private async Task HandleGameStarted(string raw)
    {
        var msg = JsonSerializer.Deserialize<GameStartedMessage>(raw, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (msg == null) return;

        // Validation
        if (msg.Pid <= 0)
        {
            _logger.LogWarning("Rejecting GameStarted: invalid pid {Pid}", msg.Pid);
            return;
        }
        if (string.IsNullOrEmpty(msg.GameId) || string.IsNullOrEmpty(msg.Name))
        {
            _logger.LogWarning("Rejecting GameStarted: missing game details");
            return;
        }

        await _eventPublisher.PublishAsync(new GameStartedEvent(msg.ClientId, msg.Pid, msg.GameId, msg.Name));
    }

    private async Task HandleGameExited(string raw)
    {
        var msg = JsonSerializer.Deserialize<GameExitedMessage>(raw, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (msg == null) return;

        // Validation
        if (string.IsNullOrEmpty(msg.GameId) || string.IsNullOrEmpty(msg.Name))
        {
            _logger.LogWarning("Rejecting GameExited: missing game details");
            return;
        }
        if (string.IsNullOrEmpty(msg.Duration) || !TimeSpan.TryParse(msg.Duration, out _))
        {
            _logger.LogWarning("Rejecting GameExited: missing or invalid duration");
            return;
        }

        await _eventPublisher.PublishAsync(new GameExitedEvent(msg.ClientId, msg.GameId, msg.Name, msg.ExitCode, msg.Duration));
    }

    private async Task HandleGameCrashed(string raw)
    {
        var msg = JsonSerializer.Deserialize<GameCrashedMessage>(raw, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (msg == null) return;

        // Validation
        if (string.IsNullOrEmpty(msg.GameId) || string.IsNullOrEmpty(msg.Name) || string.IsNullOrEmpty(msg.Reason))
        {
            _logger.LogWarning("Rejecting GameCrashed: missing game details or crash reason");
            return;
        }

        await _eventPublisher.PublishAsync(new GameCrashedEvent(msg.ClientId, msg.GameId, msg.Name, msg.ExitCode, msg.Reason));
    }

    private async Task HandleLaunchFailed(string raw)
    {
        var msg = JsonSerializer.Deserialize<LaunchFailedMessage>(raw, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (msg == null) return;

        // Validation
        if (string.IsNullOrEmpty(msg.GameId) || string.IsNullOrEmpty(msg.Name) || string.IsNullOrEmpty(msg.Reason))
        {
            _logger.LogWarning("Rejecting LaunchFailed: missing game details or failure reason");
            return;
        }

        await _eventPublisher.PublishAsync(new LaunchFailedEvent(msg.ClientId, msg.GameId, msg.Name, msg.Reason));
    }

    private void HandleHeartbeat(string raw)
    {
        var msg = JsonSerializer.Deserialize<HeartbeatMessage>(raw, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (msg == null) return;

        var client = _clientRegistry.GetById(msg.ClientId);
        if (client != null)
        {
            client.LastHeartbeat = DateTime.UtcNow;
            _clientRegistry.AddOrUpdate(client);
            _logger.LogInformation("Heartbeat received from {ClientId}", msg.ClientId);

            // Publish telemetry event as heartbeat often contains it
            _ = _eventPublisher.PublishAsync(new TelemetryReceivedEvent(msg.ClientId, 5.0f, 20.0f, 3600));
        }
    }

    private void HandlePing(string raw)
    {
        var msg = JsonSerializer.Deserialize<PingMessage>(raw, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (msg == null) return;
        _logger.LogInformation("Ping received from {ClientId}", msg.ClientId);
    }

    private void HandleClientConnected(string raw)
    {
        var msg = JsonSerializer.Deserialize<ClientConnectedMessage>(raw, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (msg == null) return;

        var client = new Client
        {
            Id = msg.ClientId,
            IPAddress = msg.IPAddress,
            Status = ClientStatus.Online,
            LastHeartbeat = DateTime.UtcNow
        };
        _clientRegistry.AddOrUpdate(client);
        _logger.LogInformation("Client connected: {ClientId} from {IPAddress}", msg.ClientId, msg.IPAddress);

        _ = _eventPublisher.PublishAsync(new ClientConnectedEvent(msg.ClientId, msg.IPAddress));
    }

    private void HandleClientDisconnected(string raw)
    {
        var msg = JsonSerializer.Deserialize<ClientDisconnectedMessage>(raw, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (msg == null) return;

        var client = _clientRegistry.GetById(msg.ClientId);
        if (client != null)
        {
            client.Status = ClientStatus.Offline;
            _clientRegistry.AddOrUpdate(client);
        }
        _logger.LogInformation("Client disconnected: {ClientId}. Reason: {Reason}", msg.ClientId, msg.Reason);
    }
}

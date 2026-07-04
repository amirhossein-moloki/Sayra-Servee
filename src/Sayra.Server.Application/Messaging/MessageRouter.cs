using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sayra.Server.Application.Interfaces;
using Sayra.Server.Domain.Entities;
using Sayra.Server.Domain.Enums;
using Sayra.Server.Shared.Messages;

namespace Sayra.Server.Application.Messaging;

public class MessageRouter : IMessageRouter
{
    private readonly ILogger<MessageRouter> _logger;
    private readonly IClientRegistry _clientRegistry;

    public MessageRouter(ILogger<MessageRouter> logger, IClientRegistry clientRegistry)
    {
        _logger = logger;
        _clientRegistry = clientRegistry;
    }

    public async Task RouteAsync(string rawMessage)
    {
        try
        {
            var baseMessage = JsonSerializer.Deserialize<BaseMessage>(rawMessage, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (baseMessage == null) return;

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

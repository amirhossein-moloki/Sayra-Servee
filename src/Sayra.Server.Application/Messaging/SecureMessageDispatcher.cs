using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sayra.Server.EventBus.Events;
using Sayra.Server.EventBus.Interfaces;
using Sayra.Server.Shared.Messages;

namespace Sayra.Server.Application.Messaging;

public interface ISecureMessageDispatcher
{
    Task DispatchAsync(BaseMessage message);
}

public class SecureMessageDispatcher : ISecureMessageDispatcher
{
    private readonly ILogger<SecureMessageDispatcher> _logger;
    private readonly IEventPublisher _eventPublisher;

    public SecureMessageDispatcher(ILogger<SecureMessageDispatcher> logger, IEventPublisher eventPublisher)
    {
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    public async Task DispatchAsync(BaseMessage message)
    {
        _logger.LogInformation("Dispatching secure message {Type} from {ClientId}", message.Type, message.ClientId);

        if (message.Type.ToUpper() == "COMMAND")
        {
            // In a real system, we'd have more details about the command execution
            await _eventPublisher.PublishAsync(new CommandExecutedEvent(
                Guid.NewGuid().ToString(),
                message.ClientId,
                "RemoteCommand",
                "Success"));
        }

        await Task.CompletedTask;
    }
}

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sayra.Server.Shared.Messages;

namespace Sayra.Server.Application.Messaging;

public interface ISecureMessageDispatcher
{
    Task DispatchAsync(BaseMessage message);
}

public class SecureMessageDispatcher : ISecureMessageDispatcher
{
    private readonly ILogger<SecureMessageDispatcher> _logger;

    public SecureMessageDispatcher(ILogger<SecureMessageDispatcher> logger)
    {
        _logger = logger;
    }

    public async Task DispatchAsync(BaseMessage message)
    {
        _logger.LogInformation("Dispatching secure message {Type} from {ClientId}", message.Type, message.ClientId);
        // Here we would actually send it to the internal event bus or handler
        await Task.CompletedTask;
    }
}

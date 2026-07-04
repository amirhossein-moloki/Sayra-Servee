using Microsoft.Extensions.Logging;
using Sayra.Server.Application.Interfaces;
using Sayra.Server.Session;
using Sayra.Server.Shared.Messages;

namespace Sayra.Server.Application.Messaging;

public class CommandAuthorizer
{
    private readonly ISessionManager _sessionManager;
    private readonly ILogger<CommandAuthorizer> _logger;

    public CommandAuthorizer(ISessionManager sessionManager, ILogger<CommandAuthorizer> logger)
    {
        _sessionManager = sessionManager;
        _logger = logger;
    }

    public bool IsAuthorized(BaseMessage message)
    {
        if (string.IsNullOrEmpty(message.ClientId)) return false;

        // AUTH and PING might be allowed without an active session in some cases,
        // but for commands, we require ACTIVE session.
        if (message.Type == "COMMAND")
        {
            if (!_sessionManager.IsSessionActive(message.ClientId))
            {
                _logger.LogWarning("Unauthorized command attempt from {ClientId}", message.ClientId);
                return false;
            }
        }

        return true;
    }
}

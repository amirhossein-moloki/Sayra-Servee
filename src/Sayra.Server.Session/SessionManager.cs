using Sayra.Server.Domain.Entities;
using Sayra.Server.EventBus.Events;
using Sayra.Server.EventBus.Interfaces;

namespace Sayra.Server.Session;

public interface ISessionManager
{
    void CreateSession(string clientId, string sessionKey);
    (Sayra.Server.Domain.Entities.Session? Session, string? SessionKey) GetSession(string clientId);
    void EndSession(string clientId);
    bool IsSessionActive(string clientId);
    void PauseSession(string clientId);
    void ResumeSession(string clientId);
}

public class SessionManager : ISessionManager
{
    private readonly ISessionRegistry _sessionRegistry;
    private readonly IEventPublisher _eventPublisher;

    public SessionManager(ISessionRegistry sessionRegistry, IEventPublisher eventPublisher)
    {
        _sessionRegistry = sessionRegistry;
        _eventPublisher = eventPublisher;
    }

    public void CreateSession(string clientId, string sessionKey)
    {
        var session = new Sayra.Server.Domain.Entities.Session
        {
            Id = Guid.NewGuid().ToString(),
            ClientId = clientId,
            StartTime = DateTime.UtcNow
        };
        _sessionRegistry.Add(clientId, session, sessionKey);

        _ = _eventPublisher.PublishAsync(new SessionStartedEvent(session.Id, clientId));
    }

    public (Sayra.Server.Domain.Entities.Session? Session, string? SessionKey) GetSession(string clientId)
    {
        return _sessionRegistry.Get(clientId);
    }

    public void EndSession(string clientId)
    {
        var (session, _) = _sessionRegistry.Get(clientId);
        _sessionRegistry.UpdateState(clientId, SessionState.Ended);
        // In a real system, we might keep it for a while before removing
        _sessionRegistry.Remove(clientId);

        if (session != null)
        {
            _ = _eventPublisher.PublishAsync(new SessionEndedEvent(session.Id, clientId, DateTime.UtcNow));
        }
    }

    public bool IsSessionActive(string clientId)
    {
        return _sessionRegistry.GetState(clientId) == SessionState.Active;
    }

    public void PauseSession(string clientId)
    {
        _sessionRegistry.UpdateState(clientId, SessionState.Paused);
    }

    public void ResumeSession(string clientId)
    {
        _sessionRegistry.UpdateState(clientId, SessionState.Active);
    }
}

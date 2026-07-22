using System.Collections.Concurrent;
using Sayra.Server.Domain.Entities;

namespace Sayra.Server.Session;

public interface ISessionRegistry
{
    void Add(string clientId, Sayra.Server.Domain.Entities.Session session, string sessionKey);
    (Sayra.Server.Domain.Entities.Session? session, string? sessionKey) Get(string clientId);
    void Remove(string clientId);
    void UpdateState(string clientId, SessionState state);
    SessionState GetState(string clientId);
    string? GetClientIdBySessionId(string sessionId);
}

public class SessionRegistry : ISessionRegistry
{
    private readonly ConcurrentDictionary<string, (Sayra.Server.Domain.Entities.Session session, string sessionKey, SessionState state)> _sessions = new();

    public void Add(string clientId, Sayra.Server.Domain.Entities.Session session, string sessionKey)
    {
        _sessions[clientId] = (session, sessionKey, SessionState.Active);
    }

    public (Sayra.Server.Domain.Entities.Session? session, string? sessionKey) Get(string clientId)
    {
        if (_sessions.TryGetValue(clientId, out var data))
        {
            return (data.session, data.sessionKey);
        }
        return (null, null);
    }

    public void Remove(string clientId)
    {
        _sessions.TryRemove(clientId, out _);
    }

    public void UpdateState(string clientId, SessionState state)
    {
        if (_sessions.TryGetValue(clientId, out var data))
        {
            _sessions[clientId] = (data.session, data.sessionKey, state);
        }
    }

    public SessionState GetState(string clientId)
    {
        if (_sessions.TryGetValue(clientId, out var data))
        {
            return data.state;
        }
        return SessionState.Idle;
    }

    public string? GetClientIdBySessionId(string sessionId)
    {
        foreach (var pair in _sessions)
        {
            if (pair.Value.session.Id == sessionId)
            {
                return pair.Key;
            }
        }
        return null;
    }
}

using System.Collections.Concurrent;

namespace Sayra.Server.Authentication;

public interface IAuthSessionManager
{
    void SetPendingChallenge(string clientId, string challenge);
    string? GetPendingChallenge(string clientId);
    void RemovePendingChallenge(string clientId);
}

public class AuthSessionManager : IAuthSessionManager
{
    private readonly ConcurrentDictionary<string, string> _pendingChallenges = new();

    public void SetPendingChallenge(string clientId, string challenge)
    {
        _pendingChallenges[clientId] = challenge;
    }

    public string? GetPendingChallenge(string clientId)
    {
        _pendingChallenges.TryGetValue(clientId, out var challenge);
        return challenge;
    }

    public void RemovePendingChallenge(string clientId)
    {
        _pendingChallenges.TryRemove(clientId, out _);
    }
}

using System.Collections.Concurrent;

namespace Sayra.Server.Security;

public interface IReplayProtectionService
{
    bool IsValid(string clientId, string nonce, long timestamp);
}

public class ReplayProtectionService : IReplayProtectionService
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, long>> _usedNonces = new();
    private const int MaxTimestampDriftSeconds = 10;
    private const int CleanupIntervalSeconds = 60;
    private long _lastCleanup = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public bool IsValid(string clientId, string nonce, long timestamp)
    {
        long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (Math.Abs(currentTimestamp - timestamp) > MaxTimestampDriftSeconds)
        {
            return false;
        }

        var clientNonces = _usedNonces.GetOrAdd(clientId, _ => new ConcurrentDictionary<string, long>());

        if (!clientNonces.TryAdd(nonce, timestamp))
        {
            return false;
        }

        CleanupIfNeeded(currentTimestamp);

        return true;
    }

    private void CleanupIfNeeded(long currentTimestamp)
    {
        if (currentTimestamp - _lastCleanup < CleanupIntervalSeconds) return;

        _lastCleanup = currentTimestamp;
        Task.Run(() =>
        {
            foreach (var clientPair in _usedNonces)
            {
                var nonces = clientPair.Value;
                foreach (var noncePair in nonces)
                {
                    if (currentTimestamp - noncePair.Value > MaxTimestampDriftSeconds * 2)
                    {
                        nonces.TryRemove(noncePair.Key, out _);
                    }
                }
                if (nonces.IsEmpty)
                {
                    _usedNonces.TryRemove(clientPair.Key, out _);
                }
            }
        });
    }
}

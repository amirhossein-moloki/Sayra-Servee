using System.Collections.Concurrent;

namespace Sayra.Server.Security;

public interface IReplayProtectionService
{
    bool IsValid(string signature, DateTime timestamp);
}

public class ReplayProtectionService : IReplayProtectionService
{
    private readonly ConcurrentDictionary<string, DateTime> _seenSignatures = new();
    private const int MaxTimestampDriftSeconds = 10;
    private DateTime _lastCleanup = DateTime.UtcNow;

    public bool IsValid(string signature, DateTime timestamp)
    {
        DateTime currentTimestamp = DateTime.UtcNow;
        if (Math.Abs((currentTimestamp - timestamp).TotalSeconds) > MaxTimestampDriftSeconds)
        {
            return false;
        }

        if (!_seenSignatures.TryAdd(signature, timestamp))
        {
            return false;
        }

        CleanupIfNeeded(currentTimestamp);
        return true;
    }

    private void CleanupIfNeeded(DateTime currentTimestamp)
    {
        if ((currentTimestamp - _lastCleanup).TotalSeconds < 60) return;

        _lastCleanup = currentTimestamp;
        Task.Run(() =>
        {
            var expired = _seenSignatures.Where(kvp =>
                (currentTimestamp - kvp.Value).TotalSeconds > MaxTimestampDriftSeconds * 2)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expired)
            {
                _seenSignatures.TryRemove(key, out _);
            }
        });
    }
}

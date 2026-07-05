namespace Sayra.Server.Scaling.State;

public interface IDistributedLock
{
    Task<bool> AcquireLockAsync(string resourceKey, TimeSpan expiration, CancellationToken ct = default);
    Task ReleaseLockAsync(string resourceKey);
}

public class RedisDistributedLock : IDistributedLock
{
    private readonly StackExchange.Redis.IDatabase _db;
    private readonly string _lockValue = Guid.NewGuid().ToString();

    public RedisDistributedLock(StackExchange.Redis.IDatabase db)
    {
        _db = db;
    }

    public async Task<bool> AcquireLockAsync(string resourceKey, TimeSpan expiration, CancellationToken ct = default)
    {
        string lockKey = $"lock:{resourceKey}";
        return await _db.LockTakeAsync(lockKey, _lockValue, expiration);
    }

    public async Task ReleaseLockAsync(string resourceKey)
    {
        string lockKey = $"lock:{resourceKey}";
        await _db.LockReleaseAsync(lockKey, _lockValue);
    }
}

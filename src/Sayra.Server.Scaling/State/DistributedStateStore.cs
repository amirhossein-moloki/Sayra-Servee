namespace Sayra.Server.Scaling.State;

public interface IDistributedStateStore
{
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task<T?> GetAsync<T>(string key);
    Task RemoveAsync(string key);
}

public class RedisDistributedStateStore : IDistributedStateStore
{
    private readonly string _connectionString;

    public RedisDistributedStateStore(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        // Redis implementation would go here using StackExchange.Redis
        await Task.CompletedTask;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        // Redis implementation would go here
        return await Task.FromResult(default(T));
    }

    public async Task RemoveAsync(string key)
    {
        // Redis implementation would go here
        await Task.CompletedTask;
    }
}

using StackExchange.Redis;
using System.Text.Json;

namespace FlutterPlatform.Infrastructure.Services;

public class RedisService : IRedisService
{
    private readonly IConnectionMultiplexer _redis;

    public RedisService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public IDatabase GetDatabase()
    {
        return _redis.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var db = GetDatabase();
        var value = await db.StringGetAsync(key);
        if (value.IsNullOrEmpty)
        {
            return default;
        }
        return JsonSerializer.Deserialize<T>(value.ToString()!);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var db = GetDatabase();
        var serialized = JsonSerializer.Serialize(value);
        await db.StringSetAsync(key, serialized, expiry.HasValue ? expiry.Value : null);
    }

    public async Task DeleteAsync(string key)
    {
        var db = GetDatabase();
        await db.KeyDeleteAsync(key);
    }

    public async Task<bool> ExistsAsync(string key)
    {
        var db = GetDatabase();
        return await db.KeyExistsAsync(key);
    }

    public async Task AddToListAsync<T>(string listKey, T value)
    {
        var db = GetDatabase();
        var serialized = JsonSerializer.Serialize(value);
        await db.ListRightPushAsync(listKey, serialized);
    }

    public async Task<T?> PopFromListAsync<T>(string listKey)
    {
        var db = GetDatabase();
        var value = await db.ListLeftPopAsync(listKey);
        if (value.IsNullOrEmpty)
        {
            return default;
        }
        return JsonSerializer.Deserialize<T>(value.ToString()!);
    }

    public async Task<long> GetListLengthAsync(string listKey)
    {
        var db = GetDatabase();
        return await db.ListLengthAsync(listKey);
    }
}

public interface IRedisService
{
    IDatabase GetDatabase();
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task DeleteAsync(string key);
    Task<bool> ExistsAsync(string key);
    Task AddToListAsync<T>(string listKey, T value);
    Task<T?> PopFromListAsync<T>(string listKey);
    Task<long> GetListLengthAsync(string listKey);
}

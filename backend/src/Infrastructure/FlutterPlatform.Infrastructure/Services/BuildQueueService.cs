using FlutterPlatform.Application.Interfaces;
using StackExchange.Redis;

namespace FlutterPlatform.Infrastructure.Services;

public class BuildQueueService : IBuildQueue
{
    private readonly IConnectionMultiplexer _redis;
    private const string QueueKey = "build:queue";

    public BuildQueueService(IConnectionMultiplexer redis) => _redis = redis;

    public async Task EnqueueAsync(Guid buildId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        await db.ListRightPushAsync(QueueKey, buildId.ToString());
    }

    public async Task<Guid?> DequeueAsync(CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var value = await db.ListLeftPopAsync(QueueKey);
        if (value.IsNullOrEmpty) return null;
        string? str = value.ToString();
        return Guid.TryParse(str, out var id) ? id : null;
    }
}

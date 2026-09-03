using System.Collections.Concurrent;
using FlutterPlatform.Application.Interfaces;

namespace FlutterPlatform.Infrastructure.Services;

public class InMemoryBuildQueue : IBuildQueue
{
    private readonly ConcurrentQueue<Guid> _queue = new();

    public Task EnqueueAsync(Guid buildId, CancellationToken ct = default)
    {
        _queue.Enqueue(buildId);
        return Task.CompletedTask;
    }

    public Task<Guid?> DequeueAsync(CancellationToken ct = default)
        => Task.FromResult(_queue.TryDequeue(out var id) ? id : (Guid?)null);
}

namespace FlutterPlatform.Application.Interfaces;

public interface IBuildQueue
{
    Task EnqueueAsync(Guid buildId, CancellationToken ct = default);
    Task<Guid?> DequeueAsync(CancellationToken ct = default);
}

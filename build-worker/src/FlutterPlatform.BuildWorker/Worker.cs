namespace FlutterPlatform.BuildWorker;

public class Worker : BackgroundService
{
    private readonly BuildJobProcessor _processor;
    private readonly ILogger<Worker> _logger;

    public Worker(BuildJobProcessor processor, ILogger<Worker> logger)
    {
        _processor = processor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Build Worker started. Polling for jobs...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var buildId = await _processor.DequeueAsync(stoppingToken);
                if (buildId.HasValue)
                {
                    _logger.LogInformation("Dequeued build {BuildId}", buildId.Value);
                    await _processor.ProcessAsync(buildId.Value, stoppingToken);
                }
                else
                {
                    // No job available, wait before polling again
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in Build Worker loop");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("Build Worker stopped.");
    }
}

namespace ThreadFlow.Api.BackgroundServices.Sync;

/// <summary>
/// Background synchronization host. Tenant-scoped push/pull behavior will be
/// implemented here and registered with the API host when the sync workflow is ready.
/// </summary>
public sealed class SyncWorker(ILogger<SyncWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("ThreadFlow synchronization host is running at: {time}", DateTimeOffset.Now);
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}

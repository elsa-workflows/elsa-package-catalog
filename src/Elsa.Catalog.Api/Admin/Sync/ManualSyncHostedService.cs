using Elsa.Catalog.Core.Sync;

namespace Elsa.Catalog.Api.Admin.Sync;

public sealed class ManualSyncHostedService(IServiceProvider services, ManualSyncQueue queue, ILogger<ManualSyncHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var workItem in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = services.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<PackageSyncService>().ExecuteManualWorkItemAsync(workItem, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                workItem.Dispose();
                return;
            }
            catch (Exception ex)
            {
                workItem.Dispose();
                logger.LogError(ex, "Manual package catalog sync failed.");
            }
        }
    }
}

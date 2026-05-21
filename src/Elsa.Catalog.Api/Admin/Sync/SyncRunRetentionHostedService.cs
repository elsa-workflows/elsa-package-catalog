using Elsa.Catalog.Core.Sync;

namespace Elsa.Catalog.Api.Admin.Sync;

public sealed class SyncRunRetentionHostedService(
    IServiceProvider services,
    IConfiguration configuration,
    TimeProvider timeProvider,
    ILogger<SyncRunRetentionHostedService> logger) : BackgroundService
{
    private const string Actor = "sync-run-retention";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enabled = configuration.GetValue("Sync:Retention:Enabled", false);
        if (!enabled)
            return;

        var retentionDays = configuration.GetValue("Sync:Retention:RetentionDays", 30);
        var interval = configuration.GetValue("Sync:Retention:Interval", TimeSpan.FromHours(24));
        var runOnStartup = configuration.GetValue("Sync:Retention:RunOnStartup", true);

        if (retentionDays <= 0)
        {
            logger.LogWarning("Sync run retention is disabled because RetentionDays is {RetentionDays}.", retentionDays);
            return;
        }

        if (interval <= TimeSpan.Zero)
        {
            logger.LogWarning("Sync run retention is disabled because Interval is {Interval}.", interval);
            return;
        }

        if (runOnStartup)
            await RunCleanupAsync(retentionDays, stoppingToken);

        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await RunCleanupAsync(retentionDays, stoppingToken);
    }

    private async Task RunCleanupAsync(int retentionDays, CancellationToken stoppingToken)
    {
        try
        {
            var cutoff = timeProvider.GetUtcNow().AddDays(-retentionDays);
            await using var scope = services.CreateAsyncScope();
            var result = await scope.ServiceProvider.GetRequiredService<SyncRunCleanupService>()
                .DeleteBeforeAsync(cutoff, Actor, stoppingToken);

            if (!result.IsValid)
            {
                logger.LogWarning("Sync run retention skipped cleanup because cutoff {CompletedBefore} was invalid.", cutoff);
                return;
            }

            logger.LogInformation(
                "Sync run retention completed: retentionDays={RetentionDays}, completedBefore={CompletedBefore}, deletedRuns={DeletedRunCount}, deletedItems={DeletedItemCount}, excludedRuns={ExcludedRunCount}",
                retentionDays,
                cutoff,
                result.Cleanup!.DeletedRunCount,
                result.Cleanup.DeletedItemCount,
                result.Cleanup.ExcludedRunCount);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Sync run retention cleanup failed.");
        }
    }
}

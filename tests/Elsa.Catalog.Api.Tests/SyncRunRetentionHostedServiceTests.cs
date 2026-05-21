using Elsa.Catalog.Api.Admin.Sync;
using Elsa.Catalog.Core.Packages;
using Elsa.Catalog.Core.Sync;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elsa.Catalog.Api.Tests;

public sealed class SyncRunRetentionHostedServiceTests
{
    [Fact]
    public async Task Startup_cleanup_deletes_terminal_runs_older_than_retention_period()
    {
        var now = DateTimeOffset.Parse("2026-05-20T12:00:00Z");
        var oldCanceled = CompletedRun(SyncRunStatus.Canceled, now.AddDays(-31), items: 1);
        var recentCompleted = CompletedRun(SyncRunStatus.Completed, now.AddDays(-10), items: 1);
        var oldRunning = new SyncRun { Trigger = SyncRunTrigger.Scheduled, Status = SyncRunStatus.Running, StartedAt = now.AddDays(-31) };
        var store = new InMemorySyncRunStore([oldCanceled, recentCompleted, oldRunning]);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Sync:Retention:Enabled"] = "true",
                ["Sync:Retention:RetentionDays"] = "30",
                ["Sync:Retention:Interval"] = "24:00:00",
                ["Sync:Retention:RunOnStartup"] = "true"
            })
            .Build();
        using var services = new ServiceCollection()
            .AddLogging()
            .AddScoped<ISyncRunStore>(_ => store)
            .AddScoped<SyncRunCleanupService>()
            .BuildServiceProvider();
        var service = new SyncRunRetentionHostedService(
            services,
            configuration,
            new FixedTimeProvider(now),
            NullLogger<SyncRunRetentionHostedService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await WaitForAsync(() => !store.Runs.Any(x => x.Id == oldCanceled.Id));
        await service.StopAsync(CancellationToken.None);

        store.Runs.Select(x => x.Id).Should().BeEquivalentTo([recentCompleted.Id, oldRunning.Id]);
        store.LastDeletedItemCount.Should().Be(1);
    }

    private static SyncRun CompletedRun(SyncRunStatus status, DateTimeOffset completedAt, int items = 0)
    {
        var run = new SyncRun
        {
            Trigger = SyncRunTrigger.Scheduled,
            Status = status,
            StartedAt = completedAt.AddMinutes(-2),
            CompletedAt = completedAt
        };

        for (var i = 0; i < items; i++)
            run.Items.Add(new SyncRunItem { SyncRun = run, SyncRunId = run.Id, Status = SyncRunItemStatus.Indexed });

        return run;
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!condition())
            await Task.Delay(25, timeout.Token);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class InMemorySyncRunStore(IReadOnlyList<SyncRun>? initialRuns = null) : ISyncRunStore
    {
        public List<SyncRun> Runs { get; } = initialRuns?.ToList() ?? [];
        public int LastDeletedItemCount { get; private set; }

        public Task<IReadOnlyList<SyncRun>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SyncRun>>(Runs);

        public Task<SyncRun?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Runs.SingleOrDefault(x => x.Id == id));

        public Task<IReadOnlyDictionary<Guid, SyncRunListMetadata>> GetListMetadataAsync(IReadOnlyCollection<Guid> runIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, SyncRunListMetadata>>(new Dictionary<Guid, SyncRunListMetadata>());

        public Task<SyncRunDeletionCandidate?> GetDeletionCandidateAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var run = Runs.SingleOrDefault(x => x.Id == id);
            return Task.FromResult(run is null ? null : new SyncRunDeletionCandidate(run.Id, run.Status, run.Items.Count));
        }

        public Task<SyncRunCleanupPreview> PreviewDeleteBeforeAsync(DateTimeOffset completedBefore, IReadOnlyCollection<SyncRunStatus> terminalStatuses, CancellationToken cancellationToken = default)
        {
            var eligible = EligibleRuns(completedBefore, terminalStatuses).ToList();
            return Task.FromResult(new SyncRunCleanupPreview(completedBefore, eligible.Count, eligible.Sum(x => x.Items.Count), 0, null, null));
        }

        public Task<SyncRunCleanupResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var run = Runs.SingleOrDefault(x => x.Id == id);
            if (run is null)
                return Task.FromResult(new SyncRunCleanupResult(0, 0, 0, 1, null, []));

            Runs.Remove(run);
            LastDeletedItemCount = run.Items.Count;
            return Task.FromResult(new SyncRunCleanupResult(1, run.Items.Count, 0, 0, null, [id]));
        }

        public Task<SyncRunCleanupResult> DeleteBeforeAsync(DateTimeOffset completedBefore, IReadOnlyCollection<SyncRunStatus> terminalStatuses, CancellationToken cancellationToken = default)
        {
            var eligible = EligibleRuns(completedBefore, terminalStatuses).ToList();
            foreach (var run in eligible)
                Runs.Remove(run);

            LastDeletedItemCount = eligible.Sum(x => x.Items.Count);
            return Task.FromResult(new SyncRunCleanupResult(
                eligible.Count,
                LastDeletedItemCount,
                0,
                0,
                completedBefore,
                eligible.Select(x => x.Id).ToList()));
        }

        public Task AddAsync(SyncRun run, CancellationToken cancellationToken = default)
        {
            Runs.Add(run);
            return Task.CompletedTask;
        }

        public Task AddItemAsync(SyncRunItem item, CancellationToken cancellationToken = default)
        {
            var run = Runs.Single(x => x.Id == item.SyncRunId);
            run.Items.Add(item);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        private IEnumerable<SyncRun> EligibleRuns(DateTimeOffset completedBefore, IReadOnlyCollection<SyncRunStatus> terminalStatuses) =>
            Runs.Where(x => x.CompletedAt.HasValue && x.CompletedAt < completedBefore && terminalStatuses.Contains(x.Status));
    }
}

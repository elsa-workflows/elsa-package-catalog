using Elsa.Catalog.Core.Packages;
using Elsa.Catalog.Core.Sync;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Catalog.Persistence.EntityFrameworkCore;

public sealed class SyncRunStore(CatalogDbContext dbContext) : ISyncRunStore
{
    public async Task<IReadOnlyList<SyncRun>> ListAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SyncRuns
            .AsNoTracking()
            .OrderByDescending(x => x.StartedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

    public Task<SyncRun?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.SyncRuns
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, SyncRunListMetadata>> GetListMetadataAsync(IReadOnlyCollection<Guid> runIds, CancellationToken cancellationToken = default)
    {
        if (runIds.Count == 0)
            return new Dictionary<Guid, SyncRunListMetadata>();

        var itemSources = await dbContext.SyncRunItems
            .AsNoTracking()
            .Where(x => runIds.Contains(x.SyncRunId))
            .Select(x => new { x.SyncRunId, x.SourceId })
            .ToListAsync(cancellationToken);

        var sourceIds = itemSources
            .Where(x => x.SourceId.HasValue)
            .Select(x => x.SourceId!.Value)
            .Distinct()
            .ToList();

        var sourceNames = await dbContext.PackageSources
            .AsNoTracking()
            .Where(x => sourceIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Name })
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        return itemSources
            .GroupBy(x => x.SyncRunId)
            .ToDictionary(
                x => x.Key,
                x => new SyncRunListMetadata(
                    x.Count(),
                    x.Where(item => item.SourceId.HasValue)
                        .Select(item => item.SourceId!.Value)
                        .Distinct()
                        .Select(sourceId => new SyncRunSourceReference(sourceId, sourceNames.GetValueOrDefault(sourceId)))
                        .OrderBy(source => source.Name ?? source.Id.ToString())
                        .ToList()));
    }

    public async Task<SyncRunDeletionCandidate?> GetDeletionCandidateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var run = await dbContext.SyncRuns
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.Status,
                ItemCount = x.Items.Count
            })
            .SingleOrDefaultAsync(cancellationToken);

        return run is null
            ? null
            : new SyncRunDeletionCandidate(run.Id, run.Status, run.ItemCount);
    }

    public async Task<SyncRunCleanupPreview> PreviewDeleteBeforeAsync(DateTimeOffset completedBefore, IReadOnlyCollection<SyncRunStatus> terminalStatuses, CancellationToken cancellationToken = default)
    {
        var terminalStatusValues = terminalStatuses.ToArray();
        var runs = await dbContext.SyncRuns
            .AsNoTracking()
            .Select(x => new { x.Id, x.Status, x.CompletedAt })
            .ToListAsync(cancellationToken);
        var eligibleRuns = runs
            .Where(x => x.CompletedAt.HasValue && x.CompletedAt < completedBefore && terminalStatusValues.Contains(x.Status))
            .ToList();

        var eligibleRunIds = eligibleRuns.Select(x => x.Id).ToList();
        var eligibleItemCount = eligibleRunIds.Count == 0
            ? 0
            : await dbContext.SyncRunItems
                .AsNoTracking()
                .CountAsync(x => eligibleRunIds.Contains(x.SyncRunId), cancellationToken);

        var excludedRunCount = runs.Count(x => !terminalStatusValues.Contains(x.Status));

        var completedAtValues = eligibleRuns
            .Select(x => x.CompletedAt)
            .OfType<DateTimeOffset>()
            .ToList();

        return new SyncRunCleanupPreview(
            completedBefore,
            eligibleRuns.Count,
            eligibleItemCount,
            excludedRunCount,
            completedAtValues.Count == 0 ? null : completedAtValues.Min(),
            completedAtValues.Count == 0 ? null : completedAtValues.Max());
    }

    public async Task<SyncRunCleanupResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var run = await dbContext.SyncRuns
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (run is null)
            return new SyncRunCleanupResult(0, 0, 0, 1, null, []);

        var itemCount = run.Items.Count;
        dbContext.SyncRuns.Remove(run);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new SyncRunCleanupResult(1, itemCount, 0, 0, null, [id]);
    }

    public async Task<SyncRunCleanupResult> DeleteBeforeAsync(DateTimeOffset completedBefore, IReadOnlyCollection<SyncRunStatus> terminalStatuses, CancellationToken cancellationToken = default)
    {
        var terminalStatusValues = terminalStatuses.ToArray();
        var allRuns = await dbContext.SyncRuns
            .Include(x => x.Items)
            .ToListAsync(cancellationToken);
        var runs = allRuns
            .Where(x => x.CompletedAt.HasValue && x.CompletedAt < completedBefore && terminalStatusValues.Contains(x.Status))
            .ToList();

        var excludedRunCount = allRuns.Count(x => !terminalStatusValues.Contains(x.Status));
        var deletedRunIds = runs.Select(x => x.Id).ToList();
        var deletedItemCount = runs.Sum(x => x.Items.Count);

        dbContext.SyncRuns.RemoveRange(runs);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new SyncRunCleanupResult(runs.Count, deletedItemCount, excludedRunCount, 0, completedBefore, deletedRunIds);
    }

    public async Task AddAsync(SyncRun run, CancellationToken cancellationToken = default) =>
        await dbContext.SyncRuns.AddAsync(run, cancellationToken);

    public async Task AddItemAsync(SyncRunItem item, CancellationToken cancellationToken = default) =>
        await dbContext.SyncRunItems.AddAsync(item, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}

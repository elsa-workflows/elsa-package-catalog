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

    public async Task AddAsync(SyncRun run, CancellationToken cancellationToken = default) =>
        await dbContext.SyncRuns.AddAsync(run, cancellationToken);

    public async Task AddItemAsync(SyncRunItem item, CancellationToken cancellationToken = default) =>
        await dbContext.SyncRunItems.AddAsync(item, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}

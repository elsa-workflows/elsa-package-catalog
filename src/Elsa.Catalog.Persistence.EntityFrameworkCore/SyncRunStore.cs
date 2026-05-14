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

    public async Task AddAsync(SyncRun run, CancellationToken cancellationToken = default) =>
        await dbContext.SyncRuns.AddAsync(run, cancellationToken);

    public async Task AddItemAsync(SyncRunItem item, CancellationToken cancellationToken = default) =>
        await dbContext.SyncRunItems.AddAsync(item, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}

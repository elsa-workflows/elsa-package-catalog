using Elsa.Catalog.Core.Packages;
using Elsa.Catalog.Core.Sources;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Catalog.Persistence.EntityFrameworkCore;

public sealed class PackageSourceStore(CatalogDbContext dbContext) : IPackageSourceStore
{
    public async Task<IReadOnlyList<PackageSource>> ListAsync(CancellationToken cancellationToken = default) =>
        await dbContext.PackageSources.OrderBy(x => x.Name).ToListAsync(cancellationToken);

    public Task<PackageSource?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.PackageSources.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddAsync(PackageSource source, CancellationToken cancellationToken = default) =>
        await dbContext.PackageSources.AddAsync(source, cancellationToken);

    public void Remove(PackageSource source) =>
        dbContext.PackageSources.Remove(source);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}

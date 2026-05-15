using Elsa.Catalog.Core.Packages;
using Elsa.Catalog.Core.Sources;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Catalog.Persistence.EntityFrameworkCore;

public sealed class PackageSourceStore(CatalogDbContext dbContext) : IPackageSourceStore
{
    public async Task<IReadOnlyList<PackageSource>> ListAsync(CancellationToken cancellationToken = default) =>
        await dbContext.PackageSources
            .Include(x => x.Packages)
            .Where(x => x.SoftDeletedAt == null)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public Task<PackageSource?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.PackageSources
            .Include(x => x.Packages)
            .SingleOrDefaultAsync(x => x.Id == id && x.SoftDeletedAt == null, cancellationToken);

    public async Task AddAsync(PackageSource source, CancellationToken cancellationToken = default) =>
        await dbContext.PackageSources.AddAsync(source, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}

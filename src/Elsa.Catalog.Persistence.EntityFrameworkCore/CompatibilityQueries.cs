using Elsa.Catalog.Core.Compatibility;
using Elsa.Catalog.Core.Packages;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Catalog.Persistence.EntityFrameworkCore;

public sealed class CompatibilityQueries(CatalogDbContext dbContext) : ICompatibilityQueries
{
    public Task<PackageVersion?> GetPackageVersionAsync(string packageId, string version, CancellationToken cancellationToken = default) =>
        dbContext.PackageVersions
            .AsNoTracking()
            .Include(x => x.Package)
            .SingleOrDefaultAsync(x => x.Package != null && x.Package.PackageId == packageId && x.Version == version, cancellationToken);
}

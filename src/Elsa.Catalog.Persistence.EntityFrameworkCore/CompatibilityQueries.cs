using Elsa.Catalog.Core.Compatibility;
using Elsa.Catalog.Core.Packages;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Catalog.Persistence.EntityFrameworkCore;

public sealed class CompatibilityQueries(CatalogDbContext dbContext) : ICompatibilityQueries
{
    public Task<PackageVersion?> GetPackageVersionAsync(Guid sourceId, string packageId, string version, CancellationToken cancellationToken = default) =>
        dbContext.PackageVersions
            .AsNoTracking()
            .Include(x => x.Package)
            .SingleOrDefaultAsync(
                x => x.Package != null
                    && x.Package.Source != null
                    && x.Package.Source.Enabled
                    && x.Package.Source.Browseable
                    && x.Package.Source.SoftDeletedAt == null
                    && x.Package.SourceId == sourceId
                    && x.Package.PackageId == packageId
                    && x.Version == version,
                cancellationToken);
}

using Elsa.Catalog.Core.Approvals;
using Elsa.Catalog.Core.Packages;
using Elsa.Catalog.Core.Sync;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Catalog.Persistence.EntityFrameworkCore;

public sealed class ApprovalStore(CatalogDbContext dbContext) : IApprovalStore
{
    public async Task<IReadOnlyList<Package>> ListPackagesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Packages
            .AsNoTracking()
            .Include(x => x.Versions)
            .ThenInclude(x => x.Features)
            .OrderBy(x => x.PackageId)
            .ToListAsync(cancellationToken);

    public Task<Package?> GetPackageAsync(string packageId, CancellationToken cancellationToken = default) =>
        dbContext.Packages
            .Include(x => x.Versions)
            .ThenInclude(x => x.Features)
            .SingleOrDefaultAsync(x => x.PackageId == packageId, cancellationToken);

    public Task<PackageVersion?> GetPackageVersionAsync(string packageId, string version, CancellationToken cancellationToken = default) =>
        dbContext.PackageVersions
            .Include(x => x.Package)
            .SingleOrDefaultAsync(x => x.Package != null && x.Package.PackageId == packageId && x.Version == version, cancellationToken);

    public async Task<IReadOnlyList<ManifestValidationResultRecord>> GetValidationResultsAsync(string packageId, string version, CancellationToken cancellationToken = default) =>
        (await dbContext.ManifestValidationResults
            .AsNoTracking()
            .Include(x => x.PackageVersion)
            .ThenInclude(x => x!.Package)
            .Where(x => x.PackageVersion != null && x.PackageVersion.Package != null && x.PackageVersion.Package.PackageId == packageId && x.PackageVersion.Version == version)
            .ToListAsync(cancellationToken))
            .OrderByDescending(x => x.ValidatedAt)
            .ToList();

    public async Task AddApprovalRecordAsync(ApprovalRecord record, CancellationToken cancellationToken = default) =>
        await dbContext.ApprovalRecords.AddAsync(record, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}

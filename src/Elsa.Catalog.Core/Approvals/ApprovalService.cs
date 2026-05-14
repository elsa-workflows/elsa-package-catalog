using Elsa.Catalog.Core.Packages;
using Elsa.Catalog.Core.Sync;

namespace Elsa.Catalog.Core.Approvals;

public sealed class ApprovalService(IApprovalStore store)
{
    public Task<IReadOnlyList<Package>> ListPackagesAsync(CancellationToken cancellationToken = default) =>
        store.ListPackagesAsync(cancellationToken);

    public Task<Package?> GetPackageAsync(string packageId, CancellationToken cancellationToken = default) =>
        store.GetPackageAsync(packageId, cancellationToken);

    public async Task<bool> SetPackageApprovalAsync(string packageId, PackageApprovalStatus status, string actor, string? reason = null, CancellationToken cancellationToken = default)
    {
        var package = await store.GetPackageAsync(packageId, cancellationToken);
        if (package is null)
            return false;

        package.Approved = status == PackageApprovalStatus.Approved;
        await store.AddApprovalRecordAsync(new ApprovalRecord
        {
            TargetType = ApprovalTargetType.Package,
            TargetId = package.Id,
            Status = status,
            Actor = actor,
            Reason = reason
        }, cancellationToken);
        await store.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetVersionApprovalAsync(string packageId, string version, PackageApprovalStatus status, string actor, string? reason = null, CancellationToken cancellationToken = default)
    {
        var packageVersion = await store.GetPackageVersionAsync(packageId, version, cancellationToken);
        if (packageVersion is null)
            return false;

        packageVersion.ApprovalStatus = status;
        await store.AddApprovalRecordAsync(new ApprovalRecord
        {
            TargetType = ApprovalTargetType.PackageVersion,
            TargetId = packageVersion.Id,
            Status = status,
            Actor = actor,
            Reason = reason
        }, cancellationToken);
        await store.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public interface IApprovalStore
{
    Task<IReadOnlyList<Package>> ListPackagesAsync(CancellationToken cancellationToken = default);
    Task<Package?> GetPackageAsync(string packageId, CancellationToken cancellationToken = default);
    Task<PackageVersion?> GetPackageVersionAsync(string packageId, string version, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ManifestValidationResultRecord>> GetValidationResultsAsync(string packageId, string version, CancellationToken cancellationToken = default);
    Task AddApprovalRecordAsync(ApprovalRecord record, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

using Elsa.Catalog.Core.Packages;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Catalog.Persistence.EntityFrameworkCore;

public sealed class PublicCatalogQueries(CatalogDbContext dbContext) : IPublicCatalogQueries
{
    public async Task<IReadOnlyList<PublicPackageProjection>> ListPackagesAsync(CancellationToken cancellationToken = default)
    {
        var packages = await VisiblePackages()
            .OrderBy(x => x.PackageId)
            .ToListAsync(cancellationToken);

        return packages.Select(ToPackageProjection).ToList();
    }

    public async Task<PublicPackageProjection?> GetPackageAsync(string packageId, CancellationToken cancellationToken = default)
    {
        var package = await VisiblePackages()
            .SingleOrDefaultAsync(x => x.PackageId == packageId, cancellationToken);

        return package is null ? null : ToPackageProjection(package);
    }

    public async Task<IReadOnlyList<PublicPackageVersionProjection>> ListVersionsAsync(string packageId, CancellationToken cancellationToken = default)
    {
        var package = await VisiblePackages()
            .SingleOrDefaultAsync(x => x.PackageId == packageId, cancellationToken);

        return package?.Versions.Select(ToVersionProjection).ToList() ?? [];
    }

    public async Task<PublicPackageVersionProjection?> GetVersionAsync(string packageId, string version, CancellationToken cancellationToken = default)
    {
        var package = await VisiblePackages()
            .SingleOrDefaultAsync(x => x.PackageId == packageId, cancellationToken);

        var packageVersion = package?.Versions.SingleOrDefault(x => x.Version == version);
        return packageVersion is null ? null : ToVersionProjection(packageVersion);
    }

    public async Task<IReadOnlyList<PublicFeatureProjection>> ListFeaturesAsync(CancellationToken cancellationToken = default)
    {
        var packages = await VisiblePackages().ToListAsync(cancellationToken);
        return packages
            .SelectMany(x => x.Versions)
            .SelectMany(x => x.Features.Select(feature => ToFeatureProjection(feature, x)))
            .OrderBy(x => x.FeatureId)
            .ToList();
    }

    public async Task<PublicFeatureProjection?> GetFeatureAsync(string featureId, CancellationToken cancellationToken = default)
    {
        var packages = await VisiblePackages().ToListAsync(cancellationToken);
        return packages
            .SelectMany(x => x.Versions)
            .SelectMany(x => x.Features.Select(feature => ToFeatureProjection(feature, x)))
            .FirstOrDefault(x => x.FeatureId == featureId);
    }

    private IQueryable<Package> VisiblePackages() =>
        dbContext.Packages
            .AsNoTracking()
            .Include(x => x.Versions.Where(version =>
                version.IsListed &&
                version.ApprovalStatus == PackageApprovalStatus.Approved &&
                version.ValidationStatus == ValidationStatus.Valid &&
                !version.SuspiciousChangeDetected))
                .ThenInclude(x => x.Features)
                .ThenInclude(x => x.Settings)
            .Where(x => x.Approved && x.Listed)
            .Where(x => x.Versions.Any(version =>
                version.IsListed &&
                version.ApprovalStatus == PackageApprovalStatus.Approved &&
                version.ValidationStatus == ValidationStatus.Valid &&
                !version.SuspiciousChangeDetected));

    private static bool IsLoadedVisibleVersion(PackageVersion version) =>
        version.IsListed &&
        version.ApprovalStatus == PackageApprovalStatus.Approved &&
        version.ValidationStatus == ValidationStatus.Valid &&
        !version.SuspiciousChangeDetected;

    private static PublicPackageProjection ToPackageProjection(Package package) =>
        new(package.PackageId, package.LatestVersion, package.Versions.Where(IsLoadedVisibleVersion).Select(ToVersionProjection).ToList());

    private static PublicPackageVersionProjection ToVersionProjection(PackageVersion version) =>
        new(
            version.Package?.PackageId ?? "",
            version.Version,
            version.SchemaVersion,
            version.PublishedAt,
            version.Features.Select(feature => ToFeatureProjection(feature, version)).ToList());

    private static PublicFeatureProjection ToFeatureProjection(Core.Manifests.FeatureRecord feature, PackageVersion version) =>
        new(
            feature.FeatureId,
            version.Package?.PackageId ?? "",
            version.Version,
            feature.TypeName,
            feature.DisplayName,
            feature.Description,
            feature.Category,
            feature.Advanced,
            feature.Experimental,
            feature.Settings
                .Select(setting => new PublicFeatureSettingProjection(
                    setting.Name,
                    setting.ClrType,
                    setting.JsonType,
                    setting.Required,
                    setting.DisplayName,
                    setting.Description,
                    setting.Category,
                    setting.Secret,
                    setting.RestartRequired,
                    setting.EnvironmentVariable))
                .ToList());
}

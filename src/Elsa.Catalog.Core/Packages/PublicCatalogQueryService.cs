using Elsa.Catalog.Core.Manifests;

namespace Elsa.Catalog.Core.Packages;

public sealed class PublicCatalogQueryService(IPublicCatalogQueries queries)
{
    public Task<IReadOnlyList<PublicPackageProjection>> ListPackagesAsync(CancellationToken cancellationToken = default) =>
        queries.ListPackagesAsync(cancellationToken);

    public Task<PublicPackageProjection?> GetPackageAsync(string packageId, CancellationToken cancellationToken = default) =>
        queries.GetPackageAsync(packageId, cancellationToken);

    public Task<IReadOnlyList<PublicPackageVersionProjection>> ListVersionsAsync(string packageId, CancellationToken cancellationToken = default) =>
        queries.ListVersionsAsync(packageId, cancellationToken);

    public Task<PublicPackageVersionProjection?> GetVersionAsync(string packageId, string version, CancellationToken cancellationToken = default) =>
        queries.GetVersionAsync(packageId, version, cancellationToken);

    public Task<IReadOnlyList<PublicFeatureProjection>> ListFeaturesAsync(CancellationToken cancellationToken = default) =>
        queries.ListFeaturesAsync(cancellationToken);

    public Task<PublicFeatureProjection?> GetFeatureAsync(string featureId, CancellationToken cancellationToken = default) =>
        queries.GetFeatureAsync(featureId, cancellationToken);
}

public interface IPublicCatalogQueries
{
    Task<IReadOnlyList<PublicPackageProjection>> ListPackagesAsync(CancellationToken cancellationToken = default);
    Task<PublicPackageProjection?> GetPackageAsync(string packageId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PublicPackageVersionProjection>> ListVersionsAsync(string packageId, CancellationToken cancellationToken = default);
    Task<PublicPackageVersionProjection?> GetVersionAsync(string packageId, string version, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PublicFeatureProjection>> ListFeaturesAsync(CancellationToken cancellationToken = default);
    Task<PublicFeatureProjection?> GetFeatureAsync(string featureId, CancellationToken cancellationToken = default);
}

public sealed record PublicPackageProjection(
    string PackageId,
    string? LatestVersion,
    IReadOnlyList<PublicPackageVersionProjection> Versions);

public sealed record PublicPackageVersionProjection(
    string PackageId,
    string Version,
    string? SchemaVersion,
    DateTimeOffset? PublishedAt,
    IReadOnlyList<PublicFeatureProjection> Features);

public sealed record PublicFeatureProjection(
    string FeatureId,
    string PackageId,
    string PackageVersion,
    string TypeName,
    string DisplayName,
    string? Description,
    string? Category,
    bool Advanced,
    bool Experimental,
    IReadOnlyList<PublicFeatureSettingProjection> Settings);

public sealed record PublicFeatureSettingProjection(
    string Name,
    string? ClrType,
    string JsonType,
    bool Required,
    string DisplayName,
    string? Description,
    string? Category,
    bool Secret,
    bool RestartRequired,
    string? EnvironmentVariable);

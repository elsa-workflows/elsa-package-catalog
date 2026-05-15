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
    PublicPackageSourceProjection Source,
    string? LatestVersion,
    IReadOnlyList<PublicPackageVersionProjection> Versions);

public sealed record PublicPackageVersionProjection(
    string PackageId,
    string Version,
    PublicPackageSourceProjection Source,
    string? SchemaVersion,
    DateTimeOffset? PublishedAt,
    IReadOnlyList<PublicFeatureProjection> Features);

public sealed record PublicPackageSourceProjection(
    Guid Id,
    string Name,
    string Url);

public sealed record PublicFeatureProjection(
    string FeatureId,
    string PackageId,
    string PackageVersion,
    PublicPackageSourceProjection Source,
    string TypeName,
    string DisplayName,
    string? Description,
    string? Category,
    IReadOnlyList<string> RequiredCapabilities,
    IReadOnlyList<PublicDependencyProjection> Dependencies,
    IReadOnlyList<PublicConflictProjection> Conflicts,
    IReadOnlyList<PublicInfrastructureRequirementProjection> Infrastructure,
    bool Advanced,
    bool Experimental,
    string ExtensionsJson,
    IReadOnlyList<PublicFeatureSettingProjection> Settings);

public sealed record PublicFeatureSettingProjection(
    string Name,
    string? ClrType,
    string JsonType,
    bool Required,
    string? DefaultValueJson,
    string DisplayName,
    string? Description,
    string? Category,
    string ValidationJson,
    bool Secret,
    bool RestartRequired,
    string? EnvironmentVariable,
    string UiJson,
    string ExtensionsJson);

public sealed record PublicDependencyProjection(
    string? PackageId,
    string? VersionRange,
    string? FeatureId,
    bool Optional,
    string? Reason);

public sealed record PublicConflictProjection(
    string? PackageId,
    string? VersionRange,
    string? FeatureId,
    string? Reason);

public sealed record PublicInfrastructureRequirementProjection(
    string Id,
    string Kind,
    bool Optional,
    string? Reason,
    IReadOnlyList<string> Capabilities,
    IReadOnlyList<string> Providers,
    IReadOnlyList<string> ConfigurationKeys,
    string ExtensionsJson);

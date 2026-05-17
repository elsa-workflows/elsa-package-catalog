using Elsa.Catalog.Core.Manifests;
using Microsoft.Extensions.Caching.Memory;

namespace Elsa.Catalog.Core.Packages;

public sealed class PublicCatalogQueryService(IPublicCatalogQueries queries, PublicCatalogCache cache)
{
    public Task<IReadOnlyList<PublicPackageProjection>> ListPackagesAsync(CancellationToken cancellationToken = default) =>
        cache.GetOrCreateAsync("packages:list", queries.ListPackagesAsync, cancellationToken);

    public Task<PublicPackageProjection?> GetPackageAsync(string packageId, CancellationToken cancellationToken = default) =>
        cache.GetOrCreateAsync($"packages:item:{packageId}", token => queries.GetPackageAsync(packageId, token), cancellationToken);

    public Task<IReadOnlyList<PublicPackageVersionProjection>> ListVersionsAsync(string packageId, CancellationToken cancellationToken = default) =>
        cache.GetOrCreateAsync($"packages:versions:{packageId}", token => queries.ListVersionsAsync(packageId, token), cancellationToken);

    public Task<PublicPackageVersionProjection?> GetVersionAsync(string packageId, string version, CancellationToken cancellationToken = default) =>
        cache.GetOrCreateAsync($"packages:version:{packageId}:{version}", token => queries.GetVersionAsync(packageId, version, token), cancellationToken);

    public Task<IReadOnlyList<PublicFeatureProjection>> ListFeaturesAsync(CancellationToken cancellationToken = default) =>
        cache.GetOrCreateAsync("features:list", queries.ListFeaturesAsync, cancellationToken);

    public Task<PublicFeatureProjection?> GetFeatureAsync(string featureId, CancellationToken cancellationToken = default) =>
        cache.GetOrCreateAsync($"features:item:{featureId}", token => queries.GetFeatureAsync(featureId, token), cancellationToken);
}

public sealed class PublicCatalogCache(IMemoryCache memoryCache) : IPublicCatalogCacheInvalidator
{
    private static readonly MemoryCacheEntryOptions CacheEntryOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
        SlidingExpiration = TimeSpan.FromMinutes(5)
    };

    private long generation;

    public Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken = default)
    {
        var generationKey = $"{Volatile.Read(ref generation)}:{key}";
        return memoryCache.GetOrCreateAsync(generationKey, entry =>
        {
            entry.SetOptions(CacheEntryOptions);
            return factory(cancellationToken);
        })!;
    }

    public void Invalidate() =>
        Interlocked.Increment(ref generation);
}

public interface IPublicCatalogCacheInvalidator
{
    void Invalidate();
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

using Elsa.Catalog.Core.Packaging;
using Elsa.Catalog.Core.Packages;
using Elsa.Catalog.Core.Sources;
using Microsoft.Extensions.Logging;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace Elsa.Catalog.Packaging.NuGet;

public sealed class NuGetPackageSourceClient(
    PackageSourcePatternMatcher patternMatcher,
    ILogger<NuGetPackageSourceClient>? logger = null) : IPackageVersionDiscoveryClient
{
    public async Task<IReadOnlyList<DiscoveredPackageVersion>> FindPackageVersionsAsync(PackageSource source, CancellationToken cancellationToken = default)
    {
        var exactPackageIds = source.IncludePatterns.Where(IsExactPackageId).ToList();
        if (exactPackageIds.Count == 0)
            throw new NotSupportedException("NuGet source discovery requires at least one exact package ID include pattern. Wildcard-only sources are not crawled.");

        var repository = Repository.Factory.GetCoreV3(source.Url);
        var resource = await repository.GetResourceAsync<FindPackageByIdResource>(cancellationToken);
        var cache = new SourceCacheContext();
        var results = new List<DiscoveredPackageVersion>();

        foreach (var packageId in exactPackageIds)
        {
            if (!patternMatcher.IsMatch(packageId, source.IncludePatterns, source.ExcludePatterns))
                continue;

            var versions = (await resource.GetAllVersionsAsync(packageId, cache, NullLogger.Instance, cancellationToken)).ToList();
            var selectedVersions = SelectVersionsForPackage(source, packageId, versions, logger);
            results.AddRange(selectedVersions.Select(version => new DiscoveredPackageVersion(packageId, version.ToNormalizedString())));
        }

        return results;
    }

    internal static IEnumerable<global::NuGet.Versioning.NuGetVersion> SelectVersions(
        PackageSourceVersionDiscoveryPolicy policy,
        IEnumerable<global::NuGet.Versioning.NuGetVersion> versions) =>
        policy switch
        {
            PackageSourceVersionDiscoveryPolicy.LatestStable => versions
                .Where(version => !version.IsPrerelease)
                .OrderDescending()
                .Take(1),
            PackageSourceVersionDiscoveryPolicy.LatestIncludingPrerelease => versions
                .OrderDescending()
                .Take(1),
            _ => versions
        };

    internal static IReadOnlyList<global::NuGet.Versioning.NuGetVersion> SelectVersionsForPackage(
        PackageSource source,
        string packageId,
        IReadOnlyList<global::NuGet.Versioning.NuGetVersion> versions,
        ILogger<NuGetPackageSourceClient>? logger = null)
    {
        var selectedVersions = SelectVersions(source.VersionDiscoveryPolicy, versions).ToList();
        if (source.VersionDiscoveryPolicy == PackageSourceVersionDiscoveryPolicy.LatestStable && versions.Count > 0 && selectedVersions.Count == 0)
            logger?.LogWarning(
                "Package {PackageId} from source {SourceName} has only prerelease versions and was skipped by the LatestStable version discovery policy.",
                packageId,
                source.Name);

        return selectedVersions;
    }

    private static bool IsExactPackageId(string pattern) =>
        !string.IsNullOrWhiteSpace(pattern) && !pattern.Contains('*') && !pattern.Contains('?');
}

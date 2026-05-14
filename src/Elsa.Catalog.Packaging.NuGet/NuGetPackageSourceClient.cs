using Elsa.Catalog.Core.Packaging;
using Elsa.Catalog.Core.Packages;
using Elsa.Catalog.Core.Sources;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace Elsa.Catalog.Packaging.NuGet;

public sealed class NuGetPackageSourceClient(PackageSourcePatternMatcher patternMatcher) : IPackageVersionDiscoveryClient
{
    public async Task<IReadOnlyList<DiscoveredPackageVersion>> FindPackageVersionsAsync(PackageSource source, CancellationToken cancellationToken = default)
    {
        var exactPackageIds = source.IncludePatterns.Where(IsExactPackageId).ToList();
        if (exactPackageIds.Count == 0)
            return [];

        var repository = Repository.Factory.GetCoreV3(source.Url);
        var resource = await repository.GetResourceAsync<FindPackageByIdResource>(cancellationToken);
        var cache = new SourceCacheContext();
        var results = new List<DiscoveredPackageVersion>();

        foreach (var packageId in exactPackageIds)
        {
            if (!patternMatcher.IsMatch(packageId, source.IncludePatterns, source.ExcludePatterns))
                continue;

            var versions = await resource.GetAllVersionsAsync(packageId, cache, NullLogger.Instance, cancellationToken);
            results.AddRange(versions.Select(version => new DiscoveredPackageVersion(packageId, version.ToNormalizedString())));
        }

        return results;
    }

    private static bool IsExactPackageId(string pattern) =>
        !string.IsNullOrWhiteSpace(pattern) && !pattern.Contains('*') && !pattern.Contains('?');
}

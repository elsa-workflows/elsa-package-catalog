using Elsa.Catalog.Core.Packaging;
using Elsa.Catalog.Core.Packages;
using Elsa.Catalog.Core.Sources;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace Elsa.Catalog.Packaging.NuGet;

public sealed class NuGetPackageSourceClient(PackageSourcePatternMatcher patternMatcher) : IPackageVersionDiscoveryClient
{
    private const int SearchPageSize = 100;
    private const int MaxSearchResultsPerPrefix = 1_000;

    public async Task<IReadOnlyList<DiscoveredPackageVersion>> FindPackageVersionsAsync(PackageSource source, CancellationToken cancellationToken = default)
    {
        var repository = Repository.Factory.GetCoreV3(source.Url);
        var packageIds = await DiscoverPackageIdsAsync(source, repository, cancellationToken);
        var resource = await repository.GetResourceAsync<FindPackageByIdResource>(cancellationToken);
        using var cache = new SourceCacheContext();
        var results = new List<DiscoveredPackageVersion>();

        foreach (var packageId in packageIds.Order(StringComparer.OrdinalIgnoreCase))
        {
            var versions = await resource.GetAllVersionsAsync(packageId, cache, NullLogger.Instance, cancellationToken);
            results.AddRange(versions.Select(version => new DiscoveredPackageVersion(packageId, version.ToNormalizedString())));
        }

        return results;
    }

    private async Task<IReadOnlyCollection<string>> DiscoverPackageIdsAsync(PackageSource source, SourceRepository repository, CancellationToken cancellationToken)
    {
        var packageIds = source.IncludePatterns
            .Where(IsExactPackageId)
            .Where(packageId => patternMatcher.IsMatch(packageId, source.IncludePatterns, source.ExcludePatterns))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var searchPrefixes = source.IncludePatterns
            .Select(GetSearchPrefix)
            .OfType<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (packageIds.Count == 0 && searchPrefixes.Count == 0)
            throw new NotSupportedException("NuGet source discovery requires at least one exact package ID include pattern or prefix wildcard include pattern, for example Elsa.*. Leading wildcard-only sources are not crawled.");

        if (searchPrefixes.Count == 0)
            return packageIds;

        var search = await repository.GetResourceAsync<PackageSearchResource>(cancellationToken);
        var filter = new SearchFilter(includePrerelease: true);

        foreach (var prefix in searchPrefixes)
        {
            for (var skip = 0; skip < MaxSearchResultsPerPrefix; skip += SearchPageSize)
            {
                var page = (await search.SearchAsync(prefix, filter, skip, SearchPageSize, NullLogger.Instance, cancellationToken)).ToList();

                foreach (var package in page)
                {
                    var packageId = package.Identity.Id;
                    if (patternMatcher.IsMatch(packageId, source.IncludePatterns, source.ExcludePatterns))
                        packageIds.Add(packageId);
                }

                if (page.Count < SearchPageSize)
                    break;

                if (skip + SearchPageSize >= MaxSearchResultsPerPrefix)
                    throw new InvalidOperationException($"NuGet source discovery for prefix '{prefix}' returned at least {MaxSearchResultsPerPrefix} packages. Narrow the include patterns before syncing.");
            }
        }

        return packageIds;
    }

    private static bool IsExactPackageId(string pattern) =>
        !string.IsNullOrWhiteSpace(pattern) && !pattern.Contains('*') && !pattern.Contains('?');

    private static string? GetSearchPrefix(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern) || IsExactPackageId(pattern))
            return null;

        var trimmed = pattern.Trim();
        var wildcardIndex = trimmed.IndexOfAny(['*', '?']);
        if (wildcardIndex <= 0)
            return null;

        return trimmed[..wildcardIndex];
    }
}

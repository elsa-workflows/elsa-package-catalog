using Elsa.Catalog.Core.Packages;
using Elsa.Catalog.Core.Sources;
using FluentAssertions;

namespace Elsa.Catalog.Packaging.NuGet.Tests;

public sealed class NuGetPackageSourceClientTests
{
    [Fact]
    public async Task Wildcard_only_sources_do_not_trigger_broad_feed_crawling()
    {
        var client = new NuGetPackageSourceClient(new PackageSourcePatternMatcher());
        var source = new PackageSource
        {
            Url = "https://example.invalid/v3/index.json",
            IncludePatterns = ["Elsa.*"]
        };

        var versions = await client.FindPackageVersionsAsync(source);

        versions.Should().BeEmpty();
    }
}

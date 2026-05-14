using System.Net.Http.Json;
using Elsa.Catalog.Api.Public.Packages;
using Elsa.Catalog.Testing;
using FluentAssertions;

namespace Elsa.Catalog.Api.Tests;

public sealed class PublicPackageDetailsApiTests
{
    [Fact]
    public async Task Get_versions_returns_visible_versions_for_package()
    {
        await using var app = new CatalogApiTestApplication();
        await app.SeedAsync(db =>
        {
            var source = PublicCatalogSeedData.CreatePackageSource();
            var package = PublicCatalogSeedData.CreatePackage(source);
            PublicCatalogSeedData.AddFeature(PublicCatalogSeedData.AddVersion(package));
            db.PackageSources.Add(source);
            return Task.CompletedTask;
        });

        var versions = await app.CreateClient().GetFromJsonAsync<List<PublicPackageVersionResponse>>("/api/packages/Elsa.Email/versions");

        versions.Should().ContainSingle(x => x.Version == "1.0.0");
    }
}

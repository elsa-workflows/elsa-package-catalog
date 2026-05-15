using Elsa.Catalog.Api.Admin.Packages;
using Elsa.Catalog.Api.Authentication;
using Elsa.Catalog.Testing;
using FluentAssertions;

namespace Elsa.Catalog.Api.Tests;

public sealed class AdminPackagesApiTests
{
    [Fact]
    public async Task Admin_can_review_visible_and_unapproved_packages()
    {
        await using var app = new CatalogApiTestApplication();
        await app.SeedAsync(db =>
        {
            var source = PublicCatalogSeedData.CreatePackageSource();
            var package = PublicCatalogSeedData.CreatePackage(source, approved: false);
            PublicCatalogSeedData.AddVersion(package);
            db.PackageSources.Add(source);
            return Task.CompletedTask;
        });
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthenticationDefaults.HeaderName, "local-dev-key");

        var packages = await client.GetCatalogJsonAsync<List<AdminPackageResponse>>("/api/admin/packages");

        packages.Should().ContainSingle(x => x.PackageId == "Elsa.Email" && !x.Approved);
    }
}

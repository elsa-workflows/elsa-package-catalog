using System.Net;
using System.Net.Http.Json;
using Elsa.Catalog.Api.Public.Packages;
using Elsa.Catalog.Core.Packages;
using Elsa.Catalog.Testing;
using FluentAssertions;

namespace Elsa.Catalog.Api.Tests;

public sealed class PublicPackagesApiTests
{
    [Fact]
    public async Task Get_packages_returns_only_visible_packages()
    {
        await using var app = new CatalogApiTestApplication();
        await app.SeedAsync(db =>
        {
            var source = PublicCatalogSeedData.CreatePackageSource();
            var visible = PublicCatalogSeedData.CreatePackage(source);
            var rejected = PublicCatalogSeedData.CreatePackage(source, "Elsa.Rejected", approved: false);

            PublicCatalogSeedData.AddFeature(PublicCatalogSeedData.AddVersion(visible));
            PublicCatalogSeedData.AddVersion(rejected);

            db.PackageSources.Add(source);
            return Task.CompletedTask;
        });

        var packages = await app.CreateClient().GetFromJsonAsync<List<PublicPackageResponse>>("/api/packages");

        packages.Should().ContainSingle(x => x.PackageId == "Elsa.Email");
        packages.Should().NotContain(x => x.PackageId == "Elsa.Rejected");
    }

    [Fact]
    public async Task Get_packages_hides_invalid_unlisted_rejected_and_suspicious_versions()
    {
        await using var app = new CatalogApiTestApplication();
        await app.SeedAsync(db =>
        {
            var source = PublicCatalogSeedData.CreatePackageSource();
            var package = PublicCatalogSeedData.CreatePackage(source);
            PublicCatalogSeedData.AddFeature(PublicCatalogSeedData.AddVersion(package));
            PublicCatalogSeedData.AddVersion(package, "1.0.1", validationStatus: ValidationStatus.Invalid);
            PublicCatalogSeedData.AddVersion(package, "1.0.2", approvalStatus: PackageApprovalStatus.Rejected);
            PublicCatalogSeedData.AddVersion(package, "1.0.3", listed: false);
            PublicCatalogSeedData.AddVersion(package, "1.0.4", suspicious: true);

            db.PackageSources.Add(source);
            return Task.CompletedTask;
        });

        var package = await app.CreateClient().GetFromJsonAsync<PublicPackageResponse>("/api/packages/Elsa.Email");

        package!.Versions.Should().ContainSingle(x => x.Version == "1.0.0");
    }

    [Fact]
    public async Task Get_package_returns_not_found_for_hidden_package()
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

        var response = await app.CreateClient().GetAsync("/api/packages/Elsa.Email");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_package_ignores_malformed_default_value_json()
    {
        await using var app = new CatalogApiTestApplication();
        await app.SeedAsync(db =>
        {
            var source = PublicCatalogSeedData.CreatePackageSource();
            var package = PublicCatalogSeedData.CreatePackage(source);
            var feature = PublicCatalogSeedData.AddFeature(PublicCatalogSeedData.AddVersion(package));
            feature.Settings[0].DefaultValueJson = "{bad";

            db.PackageSources.Add(source);
            return Task.CompletedTask;
        });

        var package = await app.CreateClient().GetFromJsonAsync<PublicPackageResponse>("/api/packages/Elsa.Email");

        package!.Versions[0].Features[0].Settings[0].DefaultValue.Should().BeNull();
    }
}

using System.Net.Http.Json;
using Elsa.Catalog.Api.Public.Compatibility;
using Elsa.Catalog.Testing;
using FluentAssertions;

namespace Elsa.Catalog.Api.Tests;

public sealed class PublicCompatibilityApiTests
{
    [Fact]
    public async Task Compatibility_check_returns_findings_for_incompatible_elsa_version()
    {
        await using var app = new CatalogApiTestApplication();
        await app.SeedAsync(db =>
        {
            var source = PublicCatalogSeedData.CreatePackageSource();
            var package = PublicCatalogSeedData.CreatePackage(source);
            var version = PublicCatalogSeedData.AddVersion(package);
            version.ManifestJson = """
            {
              "schemaVersion": "1.0",
              "package": { "id": "Elsa.Email", "version": "1.0.0" },
              "displayName": "Email",
              "compatibility": { "elsaVersionRange": "[3.0.0,4.0.0)" }
            }
            """;
            db.PackageSources.Add(source);
            return Task.CompletedTask;
        });

        var response = await app.CreateClient().PostAsJsonAsync("/api/compatibility/check", new CompatibilityCheckApiRequest(
            "4.0.0",
            null,
            [new SelectedPackageVersionApiRequest("Elsa.Email", "1.0.0")],
            []));
        var result = await response.Content.ReadFromJsonAsync<CompatibilityCheckApiResponse>();

        result!.Compatible.Should().BeFalse();
        result.Findings.Should().ContainSingle(x => x.Code == "compatibility.elsa");
    }
}

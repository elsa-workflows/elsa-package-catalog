using Elsa.Catalog.Api.Admin.Packages;
using Elsa.Catalog.Api.Authentication;
using Elsa.Catalog.Core.Packages;
using Elsa.Catalog.Core.Sync;
using Elsa.Catalog.Testing;
using FluentAssertions;

namespace Elsa.Catalog.Api.Tests;

public sealed class AdminValidationApiTests
{
    [Fact]
    public async Task Admin_can_view_validation_results_for_version()
    {
        await using var app = new CatalogApiTestApplication();
        await app.SeedAsync(db =>
        {
            var source = PublicCatalogSeedData.CreatePackageSource();
            var package = PublicCatalogSeedData.CreatePackage(source);
            var version = PublicCatalogSeedData.AddVersion(package, validationStatus: ValidationStatus.Invalid);
            db.PackageSources.Add(source);
            db.ManifestValidationResults.Add(new ManifestValidationResultRecord
            {
                PackageVersion = version,
                PackageVersionId = version.Id,
                Status = ValidationStatus.Invalid,
                SchemaVersion = "1.0",
                ErrorsJson = """["bad"]"""
            });
            return Task.CompletedTask;
        });
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthenticationDefaults.HeaderName, "local-dev-key");

        var results = await client.GetCatalogJsonAsync<List<AdminValidationResultResponse>>(
            "/api/admin/packages/Elsa.Email/versions/1.0.0/validation");

        results.Should().ContainSingle(x => x.Status == ValidationStatus.Invalid);
    }
}

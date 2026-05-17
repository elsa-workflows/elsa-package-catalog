using Elsa.Catalog.Core.Compatibility;
using Elsa.Catalog.Core.Packages;
using Elsa.Catalog.Testing;
using FluentAssertions;

namespace Elsa.Catalog.Core.Tests;

public sealed class CompatibilityCheckServiceTests
{
    [Fact]
    public async Task Reports_missing_unapproved_invalid_and_suspicious_versions()
    {
        var source = PublicCatalogSeedData.CreatePackageSource();
        var package = PublicCatalogSeedData.CreatePackage(source);
        PublicCatalogSeedData.AddVersion(package, "1.0.0", validationStatus: ValidationStatus.Invalid, approvalStatus: PackageApprovalStatus.Rejected, suspicious: true);
        var service = new CompatibilityCheckService(new FakeQueries(package.Versions), new VersionRangeEvaluator());

        var result = await service.CheckAsync(new CompatibilityCheckRequest("1.0.0", null, [new("Elsa.Email", "1.0.0"), new("Missing", "1.0.0")], []));

        result.Compatible.Should().BeFalse();
        result.Findings.Should().Contain(x => x.Code == "package.missing");
        result.Findings.Should().Contain(x => x.Code == "package.invalid");
        result.Findings.Should().Contain(x => x.Code == "package.suspicious");
        result.Findings.Should().Contain(x => x.Code == "package.notApproved");
    }

    [Fact]
    public async Task Does_not_parse_manifest_json_for_invalid_versions()
    {
        var source = PublicCatalogSeedData.CreatePackageSource();
        var package = PublicCatalogSeedData.CreatePackage(source);
        var version = PublicCatalogSeedData.AddVersion(package, validationStatus: ValidationStatus.Invalid);
        version.ManifestJson = "{";
        var service = new CompatibilityCheckService(new FakeQueries(package.Versions), new VersionRangeEvaluator());

        var result = await service.CheckAsync(new CompatibilityCheckRequest("1.0.0", null, [new("Elsa.Email", "1.0.0")], []));

        result.Compatible.Should().BeFalse();
        result.Findings.Should().ContainSingle(x => x.Code == "package.invalid");
        result.Findings.Should().NotContain(x => x.Code == "manifest.invalidJson");
    }

    [Fact]
    public async Task Reports_invalid_json_for_valid_versions_defensively()
    {
        var source = PublicCatalogSeedData.CreatePackageSource();
        var package = PublicCatalogSeedData.CreatePackage(source);
        var version = PublicCatalogSeedData.AddVersion(package);
        version.ManifestJson = "{";
        var service = new CompatibilityCheckService(new FakeQueries(package.Versions), new VersionRangeEvaluator());

        var result = await service.CheckAsync(new CompatibilityCheckRequest("1.0.0", null, [new("Elsa.Email", "1.0.0")], []));

        result.Compatible.Should().BeFalse();
        result.Findings.Should().ContainSingle(x => x.Code == "manifest.invalidJson");
    }

    [Fact]
    public async Task Reports_invalid_package_selection_without_querying_or_throwing()
    {
        var service = new CompatibilityCheckService(new FakeQueries([]), new VersionRangeEvaluator());

        var result = await service.CheckAsync(new CompatibilityCheckRequest("1.0.0", null, [new(null!, "1.0.0"), new("Elsa.Email", "")], []));

        result.Compatible.Should().BeFalse();
        result.Findings.Should().HaveCount(2);
        result.Findings.Should().OnlyContain(x => x.Code == "package.invalidSelection");
    }

    [Fact]
    public async Task Reports_missing_package_dependency_for_selected_feature()
    {
        var source = PublicCatalogSeedData.CreatePackageSource();
        var package = PublicCatalogSeedData.CreatePackage(source);
        var version = PublicCatalogSeedData.AddVersion(package);
        version.ManifestJson = """
        {
          "schemaVersion": "1.0",
          "package": { "id": "Elsa.Email", "version": "1.0.0" },
          "displayName": "Email",
          "features": [
            {
              "id": "email",
              "typeName": "Elsa.Email.EmailFeature",
              "displayName": "Email",
              "dependencies": [{ "packageId": "Elsa.Smtp" }]
            }
          ]
        }
        """;
        var service = new CompatibilityCheckService(new FakeQueries(package.Versions), new VersionRangeEvaluator());

        var result = await service.CheckAsync(new CompatibilityCheckRequest(null, null, [new("Elsa.Email", "1.0.0")], ["email"]));

        result.Findings.Should().ContainSingle(x => x.Code == "feature.packageDependency");
    }

    [Fact]
    public async Task Checks_feature_dependency_package_version_range()
    {
        var source = PublicCatalogSeedData.CreatePackageSource();
        var email = PublicCatalogSeedData.CreatePackage(source);
        var smtp = PublicCatalogSeedData.CreatePackage(source, "Elsa.Smtp");
        var emailVersion = PublicCatalogSeedData.AddVersion(email);
        PublicCatalogSeedData.AddVersion(smtp, "1.0.0");
        emailVersion.ManifestJson = """
        {
          "schemaVersion": "1.0",
          "package": { "id": "Elsa.Email", "version": "1.0.0" },
          "displayName": "Email",
          "features": [
            {
              "id": "email",
              "typeName": "Elsa.Email.EmailFeature",
              "displayName": "Email",
              "dependencies": [{ "packageId": "Elsa.Smtp", "versionRange": ">=2.0.0" }]
            }
          ]
        }
        """;
        var service = new CompatibilityCheckService(new FakeQueries(email.Versions.Concat(smtp.Versions).ToList()), new VersionRangeEvaluator());

        var result = await service.CheckAsync(new CompatibilityCheckRequest(null, null, [new("Elsa.Email", "1.0.0"), new("Elsa.Smtp", "1.0.0")], ["email"]));

        result.Findings.Should().ContainSingle(x => x.Code == "feature.packageDependency");
    }

    private sealed class FakeQueries(IReadOnlyList<PackageVersion> versions) : ICompatibilityQueries
    {
        public Task<PackageVersion?> GetPackageVersionAsync(string packageId, string version, CancellationToken cancellationToken = default) =>
            Task.FromResult(versions.SingleOrDefault(x => x.Package?.PackageId == packageId && x.Version == version));
    }
}

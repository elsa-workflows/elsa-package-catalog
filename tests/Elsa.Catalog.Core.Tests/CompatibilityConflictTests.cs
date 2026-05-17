using Elsa.Catalog.Core.Compatibility;
using Elsa.Catalog.Core.Packages;
using Elsa.Catalog.Testing;
using FluentAssertions;

namespace Elsa.Catalog.Core.Tests;

public sealed class CompatibilityConflictTests
{
    [Fact]
    public async Task Reports_direct_package_conflicts_from_manifest()
    {
        var source = PublicCatalogSeedData.CreatePackageSource();
        var email = PublicCatalogSeedData.CreatePackage(source, "Elsa.Email");
        var sms = PublicCatalogSeedData.CreatePackage(source, "Elsa.Sms");
        var emailVersion = PublicCatalogSeedData.AddVersion(email);
        PublicCatalogSeedData.AddVersion(sms);
        emailVersion.ManifestJson = """
        {
          "schemaVersion": "1.0",
          "package": { "id": "Elsa.Email", "version": "1.0.0" },
          "displayName": "Email",
          "conflicts": [{ "packageId": "Elsa.Sms" }]
        }
        """;
        var service = new CompatibilityCheckService(new FakeQueries(email.Versions.Concat(sms.Versions).ToList()), new VersionRangeEvaluator());

        var result = await service.CheckAsync(new CompatibilityCheckRequest(null, null, [Selection(source, "Elsa.Email"), Selection(source, "Elsa.Sms")], []));

        result.Findings.Should().ContainSingle(x => x.Code == "package.conflict");
    }

    [Fact]
    public async Task Ignores_package_conflicts_when_selected_version_is_outside_conflict_range()
    {
        var source = PublicCatalogSeedData.CreatePackageSource();
        var email = PublicCatalogSeedData.CreatePackage(source, "Elsa.Email");
        var sms = PublicCatalogSeedData.CreatePackage(source, "Elsa.Sms");
        var emailVersion = PublicCatalogSeedData.AddVersion(email);
        PublicCatalogSeedData.AddVersion(sms);
        emailVersion.ManifestJson = """
        {
          "schemaVersion": "1.0",
          "package": { "id": "Elsa.Email", "version": "1.0.0" },
          "displayName": "Email",
          "conflicts": [{ "packageId": "Elsa.Sms", "versionRange": ">=2.0.0" }]
        }
        """;
        var service = new CompatibilityCheckService(new FakeQueries(email.Versions.Concat(sms.Versions).ToList()), new VersionRangeEvaluator());

        var result = await service.CheckAsync(new CompatibilityCheckRequest(null, null, [Selection(source, "Elsa.Email"), Selection(source, "Elsa.Sms")], []));

        result.Findings.Should().NotContain(x => x.Code == "package.conflict");
    }

    [Fact]
    public async Task Ignores_feature_conflicts_when_selected_package_version_is_outside_conflict_range()
    {
        var source = PublicCatalogSeedData.CreatePackageSource();
        var email = PublicCatalogSeedData.CreatePackage(source, "Elsa.Email");
        var sms = PublicCatalogSeedData.CreatePackage(source, "Elsa.Sms");
        var emailVersion = PublicCatalogSeedData.AddVersion(email);
        var smsVersion = PublicCatalogSeedData.AddVersion(sms);
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
              "conflicts": [{ "packageId": "Elsa.Sms", "versionRange": ">=2.0.0", "featureId": "sms" }]
            }
          ]
        }
        """;
        smsVersion.ManifestJson = """
        {
          "schemaVersion": "1.0",
          "package": { "id": "Elsa.Sms", "version": "1.0.0" },
          "displayName": "SMS",
          "features": [
            { "id": "sms", "typeName": "Elsa.Sms.SmsFeature", "displayName": "SMS" }
          ]
        }
        """;
        var service = new CompatibilityCheckService(new FakeQueries(email.Versions.Concat(sms.Versions).ToList()), new VersionRangeEvaluator());

        var result = await service.CheckAsync(new CompatibilityCheckRequest(null, null, [Selection(source, "Elsa.Email"), Selection(source, "Elsa.Sms")], ["email", "sms"]));

        result.Findings.Should().NotContain(x => x.Code == "feature.conflict");
    }

    private static SelectedPackageVersion Selection(PackageSource source, string packageId, string version = "1.0.0") =>
        new(source.Id, packageId, version);

    private sealed class FakeQueries(IReadOnlyList<PackageVersion> versions) : ICompatibilityQueries
    {
        public Task<PackageVersion?> GetPackageVersionAsync(Guid sourceId, string packageId, string version, CancellationToken cancellationToken = default) =>
            Task.FromResult(versions.SingleOrDefault(x => x.Package?.SourceId == sourceId && x.Package.PackageId == packageId && x.Version == version));
    }
}

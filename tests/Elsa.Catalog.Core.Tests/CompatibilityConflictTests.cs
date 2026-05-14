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

        var result = await service.CheckAsync(new CompatibilityCheckRequest(null, null, [new("Elsa.Email", "1.0.0"), new("Elsa.Sms", "1.0.0")], []));

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

        var result = await service.CheckAsync(new CompatibilityCheckRequest(null, null, [new("Elsa.Email", "1.0.0"), new("Elsa.Sms", "1.0.0")], []));

        result.Findings.Should().NotContain(x => x.Code == "package.conflict");
    }

    private sealed class FakeQueries(IReadOnlyList<PackageVersion> versions) : ICompatibilityQueries
    {
        public Task<PackageVersion?> GetPackageVersionAsync(string packageId, string version, CancellationToken cancellationToken = default) =>
            Task.FromResult(versions.SingleOrDefault(x => x.Package?.PackageId == packageId && x.Version == version));
    }
}

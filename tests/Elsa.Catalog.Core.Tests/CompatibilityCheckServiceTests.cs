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

    private sealed class FakeQueries(IReadOnlyList<PackageVersion> versions) : ICompatibilityQueries
    {
        public Task<PackageVersion?> GetPackageVersionAsync(string packageId, string version, CancellationToken cancellationToken = default) =>
            Task.FromResult(versions.SingleOrDefault(x => x.Package?.PackageId == packageId && x.Version == version));
    }
}

using Elsa.Catalog.Core.Packages;
using FluentAssertions;

namespace Elsa.Catalog.Core.Tests;

public sealed class PackageVersionImmutabilityTests
{
    [Fact]
    public void CompareManifest_marks_changed_hash_as_suspicious_without_replacing_existing_hash()
    {
        var version = new PackageVersion { ManifestHash = "old" };
        var policy = new PackageVersionPolicy();

        var result = policy.CompareManifest(version, "new");

        result.IsSuspicious.Should().BeTrue();
        version.ManifestHash.Should().Be("old");
        version.SuspiciousChangeDetected.Should().BeTrue();
        version.SuspiciousManifestHash.Should().Be("new");
    }
}

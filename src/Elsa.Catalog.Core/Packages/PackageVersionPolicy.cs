namespace Elsa.Catalog.Core.Packages;

public sealed class PackageVersionPolicy
{
    public PackageVersionContentChange CompareManifest(PackageVersion existingVersion, string observedManifestHash)
    {
        if (string.Equals(existingVersion.ManifestHash, observedManifestHash, StringComparison.OrdinalIgnoreCase))
            return new PackageVersionContentChange(false, null);

        existingVersion.SuspiciousChangeDetected = true;
        existingVersion.SuspiciousManifestHash = observedManifestHash;
        return new PackageVersionContentChange(true, observedManifestHash);
    }
}

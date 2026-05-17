namespace Elsa.Catalog.Core.Packages;

public static class PackageDisplayNamePolicy
{
    private const string ElsaPackagePrefix = "Elsa.";

    public static string DefaultForPackageId(string packageId)
    {
        if (string.IsNullOrWhiteSpace(packageId))
            return packageId;

        var trimmed = packageId.Trim();
        return trimmed.StartsWith(ElsaPackagePrefix, StringComparison.OrdinalIgnoreCase)
            ? trimmed[ElsaPackagePrefix.Length..]
            : trimmed;
    }
}

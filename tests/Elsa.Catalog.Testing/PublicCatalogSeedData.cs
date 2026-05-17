using Elsa.Catalog.Core.Manifests;
using Elsa.Catalog.Core.Packages;

namespace Elsa.Catalog.Testing;

public static class PublicCatalogSeedData
{
    public static PackageSource CreatePackageSource() => new()
    {
        Name = "Test NuGet",
        Url = "https://example.test/v3/index.json",
        IncludePatterns = ["Elsa.*"],
        ApprovalPolicy = PackageSourceApprovalPolicy.Manual
    };

    public static Package CreatePackage(
        PackageSource source,
        string packageId = "Elsa.Email",
        bool approved = true,
        bool listed = true)
    {
        var package = new Package
        {
            PackageId = packageId,
            DisplayName = PackageDisplayNamePolicy.DefaultForPackageId(packageId),
            Source = source,
            SourceId = source.Id,
            Approved = approved,
            Listed = listed,
            LatestVersion = "1.0.0"
        };

        source.Packages.Add(package);
        return package;
    }

    public static PackageVersion AddVersion(
        Package package,
        string version = "1.0.0",
        ValidationStatus validationStatus = ValidationStatus.Valid,
        PackageApprovalStatus approvalStatus = PackageApprovalStatus.Approved,
        bool listed = true,
        bool suspicious = false)
    {
        var packageVersion = new PackageVersion
        {
            Package = package,
            PackageId = package.Id,
            Version = version,
            ManifestJson = new ManifestFixtureBuilder().WithPackage(package.PackageId, version).WithFeature().BuildJson(),
            ManifestHash = $"{package.PackageId}-{version}-hash",
            SchemaVersion = "1.0",
            ValidationStatus = validationStatus,
            ApprovalStatus = approvalStatus,
            IsListed = listed,
            SuspiciousChangeDetected = suspicious,
            PublishedAt = DateTimeOffset.UtcNow
        };

        package.Versions.Add(packageVersion);
        return packageVersion;
    }

    public static FeatureRecord AddFeature(
        PackageVersion version,
        string featureId = "email",
        string displayName = "Email")
    {
        var feature = new FeatureRecord
        {
            PackageVersion = version,
            PackageVersionId = version.Id,
            FeatureId = featureId,
            TypeName = $"Elsa.Features.{featureId}.Feature",
            DisplayName = displayName,
            Description = $"{displayName} feature.",
            Category = "Communication"
        };

        feature.Settings.Add(new FeatureSettingRecord
        {
            FeatureRecord = feature,
            FeatureRecordId = feature.Id,
            Name = "smtpHost",
            ClrType = "System.String",
            JsonType = "string",
            Required = true,
            DisplayName = "SMTP host",
            Category = "Connection"
        });

        version.Features.Add(feature);
        return feature;
    }
}

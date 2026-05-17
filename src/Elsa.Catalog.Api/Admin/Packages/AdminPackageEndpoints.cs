using Elsa.Catalog.Api.Authentication;
using Elsa.Catalog.Core.Approvals;
using Elsa.Catalog.Core.Manifests;
using Elsa.Catalog.Core.Packages;
using Elsa.PackageManifests;
using System.Text.Json;

namespace Elsa.Catalog.Api.Admin.Packages;

public static class AdminPackageEndpoints
{
    public static IEndpointRouteBuilder MapAdminPackageEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/admin/packages")
            .RequireAuthorization(AdminAuthorization.Policy)
            .WithTags("Admin Packages");

        group.MapGet("/", async (ApprovalService approvals, CancellationToken cancellationToken) =>
            Results.Ok((await approvals.ListPackagesAsync(cancellationToken)).Select(ToResponse)));

        group.MapGet("/{packageId}", async (string packageId, ApprovalService approvals, CancellationToken cancellationToken) =>
        {
            var package = await approvals.GetPackageAsync(packageId, cancellationToken);
            return package is null ? Results.NotFound() : Results.Ok(ToResponse(package));
        });

        return endpoints;
    }

    internal static AdminPackageResponse ToResponse(Package package)
    {
        var latestVersion = package.Versions.FirstOrDefault(version => version.Version == package.LatestVersion) ?? package.Versions.FirstOrDefault();

        return new(
            package.PackageId,
            package.Approved,
            package.Listed,
            package.SourceId,
            package.Source is null ? null : new AdminPackageSourceResponse(
                package.Source.Id,
                package.Source.Name,
                package.Source.Url,
                package.Source.Enabled,
                package.Source.Status,
                package.Source.LastSyncedAt,
                package.Source.LastSuccessfulSyncAt),
            package.LatestVersion,
            ToApprovalStatus(package, latestVersion),
            ToValidationStatus(latestVersion),
            latestVersion?.Features.Count ?? 0,
            package.CreatedAt,
            package.UpdatedAt,
            package.Versions.Select(ToVersionResponse).ToList());
    }

    private static PackageApprovalStatus ToApprovalStatus(Package package, PackageVersion? latestVersion) =>
        latestVersion?.ApprovalStatus ?? (package.Approved ? PackageApprovalStatus.Approved : PackageApprovalStatus.Pending);

    private static ValidationStatus ToValidationStatus(PackageVersion? latestVersion) =>
        latestVersion?.ValidationStatus ?? ValidationStatus.NotValidated;

    private static AdminPackageVersionResponse ToVersionResponse(PackageVersion version)
    {
        var manifest = ReadManifest(version.ManifestJson);
        var compatibility = manifest?.Compatibility;
        var requiredCapabilities = version.Features.SelectMany(x => DeserializeStringList(x.RequiredCapabilitiesJson)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        return new(
            version.Version,
            version.ValidationStatus,
            version.ApprovalStatus,
            version.IsListed,
            version.SuspiciousChangeDetected,
            version.SchemaVersion,
            version.ManifestHash,
            version.SuspiciousManifestHash,
            ApprovalService.CreateVersionStateToken(version),
            version.PublishedAt,
            version.IndexedAt,
            version.Features.Count,
            version.Features.Sum(x => x.Settings.Count),
            new AdminCompatibilityResponse(
                compatibility?.DockerImageVersionRange is null ? [] : [compatibility.DockerImageVersionRange],
                compatibility?.ElsaVersionRange,
                (compatibility?.RuntimeCapabilities ?? []).Concat(requiredCapabilities).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                compatibility?.PackageRules.Select(x => x.Reason).Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>().ToList() ?? [],
                compatibility?.PackageRules.Select(x => $"{x.PackageId} {x.VersionRange}".Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ?? []),
            VisibilityReasons(version),
            version.Features.Select(ToFeatureResponse).ToList(),
            new AdminManifestResponse(
                !string.IsNullOrWhiteSpace(version.ManifestJson),
                version.SchemaVersion,
                version.ManifestHash,
                version.SuspiciousManifestHash,
                version.ManifestJson));
    }

    private static AdminFeatureResponse ToFeatureResponse(FeatureRecord feature) =>
        new(
            feature.FeatureId,
            feature.TypeName,
            feature.DisplayName,
            feature.Description,
            feature.Category,
            DeserializeStringList(feature.RequiredCapabilitiesJson),
            feature.DependenciesJson,
            feature.ConflictsJson,
            feature.InfrastructureJson,
            feature.Advanced,
            feature.Experimental,
            feature.ExtensionsJson,
            feature.Settings.Select(setting => new AdminFeatureSettingResponse(
                setting.Name,
                setting.ClrType,
                setting.JsonType,
                setting.Required,
                setting.DefaultValueJson,
                setting.DisplayName,
                setting.Description,
                setting.Category,
                setting.ValidationJson,
                setting.Secret,
                setting.RestartRequired,
                setting.EnvironmentVariable,
                setting.UiJson,
                setting.ExtensionsJson)).ToList());

    private static IReadOnlyList<AdminVisibilityReasonResponse> VisibilityReasons(PackageVersion version)
    {
        var reasons = new List<AdminVisibilityReasonResponse>();
        if (version.ApprovalStatus == PackageApprovalStatus.Pending)
            reasons.Add(Block("VersionPendingApproval", "TrustDecision", "This package version is pending approval."));
        if (version.ApprovalStatus == PackageApprovalStatus.Rejected)
            reasons.Add(Block("VersionRejected", "TrustDecision", "This package version is rejected."));
        if (version.ValidationStatus != ValidationStatus.Valid)
            reasons.Add(Block("ValidationNotValid", "Validation", $"Validation status is {version.ValidationStatus}."));
        if (!version.IsListed)
            reasons.Add(Block("VersionUnlisted", "Listing", "This package version is unlisted."));
        if (version.SuspiciousChangeDetected)
            reasons.Add(Block("SuspiciousManifestChange", "Manifest", "This immutable package version produced different manifest content."));
        if (string.IsNullOrWhiteSpace(version.ManifestJson))
            reasons.Add(Block("ManifestMissing", "Manifest", "Manifest content is missing."));

        return reasons.Count == 0
            ? [new("Visible", "TrustDecision", "Info", "This package version is approved, valid, and listed.", false)]
            : reasons;
    }

    private static AdminVisibilityReasonResponse Block(string code, string category, string message) =>
        new(code, category, "Blocking", message, true);

    private static ElsaPackageManifest? ReadManifest(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<ElsaPackageManifest>(json, ManifestJsonSerializerOptions.Default);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static IReadOnlyList<string> DeserializeStringList(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<IReadOnlyList<string>>(json, ManifestJsonSerializerOptions.Default) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}

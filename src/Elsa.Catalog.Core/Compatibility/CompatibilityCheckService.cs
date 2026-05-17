using System.Text.Json;
using Elsa.Catalog.Core.Packages;
using Elsa.PackageManifests;

namespace Elsa.Catalog.Core.Compatibility;

public sealed class CompatibilityCheckService(ICompatibilityQueries queries, VersionRangeEvaluator ranges)
{
    public async Task<CompatibilityCheckResult> CheckAsync(CompatibilityCheckRequest request, CancellationToken cancellationToken = default)
    {
        var findings = new List<CompatibilityFinding>();
        var selected = new List<(SelectedPackageIdentity Identity, ElsaPackageManifest Manifest)>();

        var validPackages = new List<SelectedPackageVersion>();
        for (var index = 0; index < request.Packages.Count; index++)
        {
            var package = request.Packages[index];
            if (package.SourceId == Guid.Empty || string.IsNullOrWhiteSpace(package.PackageId) || string.IsNullOrWhiteSpace(package.Version))
            {
                findings.Add(CompatibilityFinding.Error("package.invalidSelection", $"Package selection at index {index} requires sourceId, packageId, and version."));
                continue;
            }

            validPackages.Add(package);
        }

        foreach (var package in validPackages)
        {
            var version = await queries.GetPackageVersionAsync(package.SourceId, package.PackageId, package.Version, cancellationToken);
            if (version is null)
            {
                findings.Add(CompatibilityFinding.Error("package.missing", $"{package.PackageId} {package.Version} is not indexed."));
                continue;
            }

            if (version.Package is not { Approved: true, Listed: true } || !version.IsListed || version.ApprovalStatus != PackageApprovalStatus.Approved)
                findings.Add(CompatibilityFinding.Error("package.notApproved", $"{package.PackageId} {package.Version} is not approved and listed."));

            if (version.SuspiciousChangeDetected)
                findings.Add(CompatibilityFinding.Error("package.suspicious", $"{package.PackageId} {package.Version} has a suspicious manifest change."));

            if (version.ValidationStatus != ValidationStatus.Valid)
            {
                findings.Add(CompatibilityFinding.Error("package.invalid", $"{package.PackageId} {package.Version} does not have a valid manifest."));
                continue;
            }

            if (!TryParseManifest(version.ManifestJson, out var manifest))
            {
                findings.Add(CompatibilityFinding.Error("manifest.invalidJson", $"{package.PackageId} {package.Version} has invalid manifest JSON."));
                continue;
            }

            selected.Add((new SelectedPackageIdentity(package.SourceId, package.PackageId), manifest!));

            if (manifest?.Compatibility?.ElsaVersionRange is { } elsaRange && !ranges.Includes(elsaRange, request.ElsaVersion))
                findings.Add(CompatibilityFinding.Error("compatibility.elsa", $"{package.PackageId} {package.Version} is not compatible with Elsa {request.ElsaVersion}."));

            if (manifest?.Compatibility?.DockerImageVersionRange is { } dockerRange && !ranges.Includes(dockerRange, request.DockerImageVersion))
                findings.Add(CompatibilityFinding.Error("compatibility.docker", $"{package.PackageId} {package.Version} is not compatible with Docker image {request.DockerImageVersion}."));
        }

        var selectedVersions = validPackages
            .GroupBy(x => new SelectedPackageIdentity(x.SourceId, x.PackageId))
            .ToDictionary(x => x.Key, x => x.Select(package => package.Version).ToList());
        foreach (var (identity, manifest) in selected)
        {
            foreach (var conflict in manifest.Conflicts)
            {
                if (conflict.PackageId is not null
                    && selectedVersions.TryGetValue(new SelectedPackageIdentity(identity.SourceId, conflict.PackageId), out var conflictingVersions)
                    && conflictingVersions.Any(selectedVersion => ranges.Includes(conflict.VersionRange, selectedVersion)))
                {
                    findings.Add(CompatibilityFinding.Error("package.conflict", $"{manifest.Package.Id} conflicts with {conflict.PackageId}."));
                }
            }
        }

        if (request.Features.Count > 0)
            ValidateSelectedFeatures(request.Features, selected, selectedVersions, ranges, findings);

        return new CompatibilityCheckResult(findings.Count == 0, findings);
    }

    private static void ValidateSelectedFeatures(
        IReadOnlyList<string> selectedFeatureIds,
        IReadOnlyList<(SelectedPackageIdentity Identity, ElsaPackageManifest Manifest)> selected,
        IReadOnlyDictionary<SelectedPackageIdentity, List<string>> selectedVersions,
        VersionRangeEvaluator ranges,
        List<CompatibilityFinding> findings)
    {
        var selectedFeatures = new HashSet<string>(selectedFeatureIds, StringComparer.OrdinalIgnoreCase);
        var features = selected
            .SelectMany(package => package.Manifest.Features.Select(feature => new SelectedFeatureManifest(package.Identity.SourceId, package.Manifest.Package.Id, package.Manifest.Package.Version, feature)))
            .Where(x => selectedFeatures.Contains(x.Id))
            .ToList();

        foreach (var requestedFeatureId in selectedFeatures)
        {
            if (features.All(x => !string.Equals(x.Id, requestedFeatureId, StringComparison.OrdinalIgnoreCase)))
                findings.Add(CompatibilityFinding.Error("feature.missing", $"Feature {requestedFeatureId} is not present in the selected packages."));
        }

        foreach (var selectedFeature in features)
        {
            var feature = selectedFeature.Feature;
            foreach (var dependency in feature.Dependencies.Where(x => !x.Optional))
            {
                if (dependency.PackageId is not null && !PackageMatches(selectedFeature.SourceId, dependency.PackageId, dependency.VersionRange, selectedVersions, ranges))
                {
                    findings.Add(CompatibilityFinding.Error("feature.packageDependency", $"{feature.Id} requires package {dependency.PackageId}."));
                    continue;
                }

                if (dependency.FeatureId is not null && !FeatureMatches(selectedFeature.SourceId, dependency.PackageId, dependency.VersionRange, dependency.FeatureId, features, ranges))
                    findings.Add(CompatibilityFinding.Error("feature.dependency", $"{feature.Id} requires feature {dependency.FeatureId}."));
            }

            foreach (var conflict in feature.Conflicts)
            {
                if (conflict.PackageId is not null && !PackageMatches(selectedFeature.SourceId, conflict.PackageId, conflict.VersionRange, selectedVersions, ranges))
                    continue;

                if (conflict.FeatureId is null || FeatureMatches(selectedFeature.SourceId, conflict.PackageId, conflict.VersionRange, conflict.FeatureId, features, ranges))
                    findings.Add(CompatibilityFinding.Error("feature.conflict", $"{feature.Id} conflicts with feature {conflict.FeatureId}."));
            }
        }
    }

    private static bool PackageMatches(Guid sourceId, string packageId, string? versionRange, IReadOnlyDictionary<SelectedPackageIdentity, List<string>> selectedVersions, VersionRangeEvaluator ranges) =>
        selectedVersions.TryGetValue(new SelectedPackageIdentity(sourceId, packageId), out var versions) && versions.Any(version => ranges.Includes(versionRange, version));

    private static bool FeatureMatches(Guid sourceId, string? packageId, string? versionRange, string featureId, IReadOnlyList<SelectedFeatureManifest> features, VersionRangeEvaluator ranges) =>
        features.Any(feature =>
            string.Equals(feature.Id, featureId, StringComparison.OrdinalIgnoreCase)
            && (packageId is null || feature.SourceId == sourceId)
            && (packageId is null || string.Equals(feature.PackageId, packageId, StringComparison.OrdinalIgnoreCase))
            && (packageId is null || ranges.Includes(versionRange, feature.PackageVersion)));

    private static bool TryParseManifest(string manifestJson, out ElsaPackageManifest? manifest)
    {
        try
        {
            manifest = JsonSerializer.Deserialize<ElsaPackageManifest>(manifestJson, ManifestJsonSerializerOptions.Default);
            return manifest is not null;
        }
        catch (JsonException)
        {
            manifest = null;
            return false;
        }
    }
}

public interface ICompatibilityQueries
{
    Task<PackageVersion?> GetPackageVersionAsync(Guid sourceId, string packageId, string version, CancellationToken cancellationToken = default);
}

public sealed record CompatibilityCheckRequest(
    string? ElsaVersion,
    string? DockerImageVersion,
    IReadOnlyList<SelectedPackageVersion> Packages,
    IReadOnlyList<string> Features);

public sealed record SelectedPackageVersion(Guid SourceId, string PackageId, string Version);

public sealed record CompatibilityCheckResult(bool Compatible, IReadOnlyList<CompatibilityFinding> Findings);

public sealed record CompatibilityFinding(string Severity, string Code, string Message)
{
    public static CompatibilityFinding Error(string code, string message) => new("error", code, message);
    public static CompatibilityFinding Warning(string code, string message) => new("warning", code, message);
}

internal sealed record SelectedPackageIdentity(Guid SourceId, string PackageId);

internal sealed record SelectedFeatureManifest(Guid SourceId, string PackageId, string PackageVersion, FeatureManifest Feature)
{
    public string Id => Feature.Id;
}

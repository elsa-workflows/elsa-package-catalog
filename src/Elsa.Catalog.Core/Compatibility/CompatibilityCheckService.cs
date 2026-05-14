using System.Text.Json;
using Elsa.Catalog.Core.Packages;
using Elsa.PackageManifests;

namespace Elsa.Catalog.Core.Compatibility;

public sealed class CompatibilityCheckService(ICompatibilityQueries queries, VersionRangeEvaluator ranges)
{
    public async Task<CompatibilityCheckResult> CheckAsync(CompatibilityCheckRequest request, CancellationToken cancellationToken = default)
    {
        var findings = new List<CompatibilityFinding>();
        var selected = new List<(PackageVersion Version, ElsaPackageManifest Manifest)>();

        foreach (var package in request.Packages)
        {
            var version = await queries.GetPackageVersionAsync(package.PackageId, package.Version, cancellationToken);
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

            selected.Add((version, manifest!));

            if (manifest?.Compatibility?.ElsaVersionRange is { } elsaRange && !ranges.Includes(elsaRange, request.ElsaVersion))
                findings.Add(CompatibilityFinding.Error("compatibility.elsa", $"{package.PackageId} {package.Version} is not compatible with Elsa {request.ElsaVersion}."));

            if (manifest?.Compatibility?.DockerImageVersionRange is { } dockerRange && !ranges.Includes(dockerRange, request.DockerImageVersion))
                findings.Add(CompatibilityFinding.Error("compatibility.docker", $"{package.PackageId} {package.Version} is not compatible with Docker image {request.DockerImageVersion}."));
        }

        var selectedVersions = request.Packages
            .GroupBy(x => x.PackageId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Select(package => package.Version).ToList(), StringComparer.OrdinalIgnoreCase);
        foreach (var (_, manifest) in selected)
        {
            foreach (var conflict in manifest.Conflicts)
            {
                if (conflict.PackageId is not null
                    && selectedVersions.TryGetValue(conflict.PackageId, out var conflictingVersions)
                    && conflictingVersions.Any(selectedVersion => ranges.Includes(conflict.VersionRange, selectedVersion)))
                {
                    findings.Add(CompatibilityFinding.Error("package.conflict", $"{manifest.Package.Id} conflicts with {conflict.PackageId}."));
                }
            }
        }

        return new CompatibilityCheckResult(findings.Count == 0, findings);
    }

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
    Task<PackageVersion?> GetPackageVersionAsync(string packageId, string version, CancellationToken cancellationToken = default);
}

public sealed record CompatibilityCheckRequest(
    string? ElsaVersion,
    string? DockerImageVersion,
    IReadOnlyList<SelectedPackageVersion> Packages,
    IReadOnlyList<string> Features);

public sealed record SelectedPackageVersion(string PackageId, string Version);

public sealed record CompatibilityCheckResult(bool Compatible, IReadOnlyList<CompatibilityFinding> Findings);

public sealed record CompatibilityFinding(string Severity, string Code, string Message)
{
    public static CompatibilityFinding Error(string code, string message) => new("error", code, message);
    public static CompatibilityFinding Warning(string code, string message) => new("warning", code, message);
}

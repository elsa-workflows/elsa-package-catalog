using System.Text.Json;
using Elsa.Catalog.Core.Manifests;
using Elsa.Catalog.Core.Packaging;
using Elsa.Catalog.Core.Packages;
using Elsa.Catalog.Core.Sources;
using Elsa.PackageManifests;
using Elsa.PackageManifests.Validation;
using NuGet.Versioning;

namespace Elsa.Catalog.Core.Sync;

public sealed class PackageSyncService(
    IPackageSourceStore sources,
    ISyncCatalogStore catalog,
    ISyncRunStore syncRuns,
    IPackageVersionDiscoveryClient discovery,
    IPackageArchiveDownloader downloader,
    IPackageArchiveManifestReader manifestReader,
    ManifestValidator validator,
    ManifestIngestionService ingestion,
    PackageVersionPolicy versionPolicy,
    ISyncDiagnostics diagnostics,
    SyncConcurrencyGuard concurrencyGuard)
{
    public async Task<SyncRun> SyncAllAsync(CancellationToken cancellationToken = default)
    {
        SyncRun? run = null;
        var executed = await concurrencyGuard.TryRunAsync("sync", async () =>
        {
            run = new SyncRun { Trigger = SyncRunTrigger.ManualAll };
            await ExecuteRunAsync(run, () => sources.ListAsync(cancellationToken), cancellationToken);
        });

        return executed
            ? run!
            : RejectedRun(SyncRunTrigger.ManualAll, "A sync run is already active for all sources.");
    }

    public async Task<SyncRun> SyncSourceAsync(Guid sourceId, CancellationToken cancellationToken = default)
    {
        SyncRun? run = null;
        var executed = await concurrencyGuard.TryRunAsync("sync", async () =>
        {
            run = new SyncRun { Trigger = SyncRunTrigger.ManualSource };
            await ExecuteRunAsync(run, async () =>
            {
                var source = await sources.GetAsync(sourceId, cancellationToken);
                return source is null ? [] : [source];
            }, cancellationToken);
        });

        return executed
            ? run!
            : RejectedRun(SyncRunTrigger.ManualSource, $"A sync run is already active for source '{sourceId}'.");
    }

    private async Task ExecuteRunAsync(SyncRun run, Func<Task<IReadOnlyList<PackageSource>>> getSources, CancellationToken cancellationToken)
    {
        diagnostics.SyncRunStarted(run.Id);
        await syncRuns.AddAsync(run, cancellationToken);
        await syncRuns.SaveChangesAsync(cancellationToken);

        var counters = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        try
        {
            foreach (var source in (await getSources()).Where(x => x.Enabled))
            {
                await SyncSourceAsync(run, source, counters, cancellationToken);
                await sources.SaveChangesAsync(cancellationToken);
            }

            run.Status = run.Items.Any(x => x.Status == SyncRunItemStatus.Failed)
                ? SyncRunStatus.CompletedWithErrors
                : SyncRunStatus.Completed;
        }
        catch (Exception ex)
        {
            run.Status = SyncRunStatus.Failed;
            run.Error = ex.Message;
        }
        finally
        {
            run.CompletedAt = DateTimeOffset.UtcNow;
            run.SummaryCountersJson = JsonSerializer.Serialize(counters);
            await syncRuns.SaveChangesAsync(CancellationToken.None);
            diagnostics.SyncRunCompleted(run.Id, run.Status);
        }
    }

    private async Task SyncSourceAsync(SyncRun run, PackageSource source, Dictionary<string, int> counters, CancellationToken cancellationToken)
    {
        IReadOnlyList<DiscoveredPackageVersion> discovered;
        try
        {
            discovered = await discovery.FindPackageVersionsAsync(source, cancellationToken);
        }
        catch (Exception ex)
        {
            await AddItemAsync(run, source.Id, null, null, SyncRunItemStatus.Failed, cancellationToken, error: ex.Message);
            Increment(counters, "failed");
            source.LastSyncedAt = DateTimeOffset.UtcNow;
            source.Status = PackageSourceStatus.Error;
            source.LastSyncError = LimitFailureDetail(ex.Message);
            return;
        }

        var failureCount = 0;
        SyncRunItem? firstFailure = null;
        foreach (var item in discovered)
        {
            var runItem = await SyncPackageVersionAsync(run, source, item, counters, cancellationToken);
            if (runItem.Status != SyncRunItemStatus.Failed)
                continue;

            failureCount++;
            firstFailure ??= runItem;
        }

        source.LastSyncedAt = DateTimeOffset.UtcNow;
        var hasFailures = failureCount > 0;
        source.Status = hasFailures ? PackageSourceStatus.Warning : PackageSourceStatus.Healthy;
        source.LastSyncError = hasFailures ? BuildSourceFailureDetail(firstFailure, failureCount) : null;
        if (!hasFailures)
            source.LastSuccessfulSyncAt = source.LastSyncedAt;
    }

    private async Task<SyncRunItem> SyncPackageVersionAsync(SyncRun run, PackageSource source, DiscoveredPackageVersion discovered, Dictionary<string, int> counters, CancellationToken cancellationToken)
    {
        var runItem = await AddItemAsync(run, source.Id, discovered.PackageId, discovered.Version, SyncRunItemStatus.Discovered, cancellationToken);
        try
        {
            var package = await catalog.GetPackageAsync(source.Id, discovered.PackageId, cancellationToken)
                ?? new Package
                {
                    SourceId = source.Id,
                    PackageId = discovered.PackageId,
                    Approved = source.ApprovalPolicy == PackageSourceApprovalPolicy.AutoApprove,
                    Listed = true
                };

            var existingVersion = await catalog.GetPackageVersionAsync(package.Id, discovered.Version, cancellationToken);
            await using var packageStream = await downloader.DownloadPackageAsync(source, discovered.PackageId, discovered.Version, cancellationToken);
            var read = await manifestReader.ReadAsync(packageStream, cancellationToken);
            if (!read.Exists || read.ManifestJson is null || read.ManifestHash is null)
            {
                runItem.Status = SyncRunItemStatus.Invalid;
                runItem.Message = "Package does not contain elsa-package.json.";
                Increment(counters, "invalid");
                return runItem;
            }

            UpdateLatestVersion(package, discovered.Version);
            if (existingVersion is not null)
            {
                var change = versionPolicy.CompareManifest(existingVersion, read.ManifestHash);
                if (change.IsSuspicious)
                {
                    existingVersion.SuspiciousChangeDetected = true;
                    existingVersion.SuspiciousManifestHash = change.ObservedHash;
                    existingVersion.ValidationStatus = ValidationStatus.Suspicious;
                    runItem.Status = SyncRunItemStatus.Suspicious;
                    runItem.PackageVersion = existingVersion;
                    runItem.PackageVersionId = existingVersion.Id;
                    diagnostics.SuspiciousManifestChange(run.Id, discovered.PackageId, discovered.Version, read.ManifestHash);
                    Increment(counters, "suspicious");
                }
                else
                {
                    runItem.Status = SyncRunItemStatus.Unchanged;
                    runItem.PackageVersion = existingVersion;
                    runItem.PackageVersionId = existingVersion.Id;
                    Increment(counters, "unchanged");
                }

                return runItem;
            }

            var validation = validator.Validate(read.ManifestJson, discovered.PackageId, discovered.Version);
            var schemaVersion = ExtractSchemaVersion(read.ManifestJson);
            var packageVersion = new PackageVersion
            {
                Package = package,
                PackageId = package.Id,
                Version = discovered.Version,
                ManifestJson = read.ManifestJson,
                ManifestHash = read.ManifestHash,
                PublishedAt = discovered.PublishedAt,
                ValidationStatus = ToValidationStatus(validation),
                ValidationErrors = JsonSerializer.Serialize(validation.Errors, ManifestJsonSerializerOptions.Default),
                ApprovalStatus = source.ApprovalPolicy == PackageSourceApprovalPolicy.AutoApprove ? PackageApprovalStatus.Approved : PackageApprovalStatus.Pending,
                IsListed = true,
                SchemaVersion = schemaVersion
            };

            if (packageVersion.ValidationStatus == ValidationStatus.Valid)
                ingestion.Ingest(packageVersion, read.ManifestJson);

            package.Versions.Add(packageVersion);

            if (await catalog.GetPackageAsync(source.Id, discovered.PackageId, cancellationToken) is null)
                await catalog.AddPackageAsync(package, cancellationToken);

            await catalog.AddValidationResultAsync(new ManifestValidationResultRecord
            {
                PackageVersion = packageVersion,
                PackageVersionId = packageVersion.Id,
                SchemaVersion = schemaVersion,
                Status = packageVersion.ValidationStatus,
                ErrorsJson = JsonSerializer.Serialize(validation.Errors, ManifestJsonSerializerOptions.Default),
                WarningsJson = JsonSerializer.Serialize(validation.Warnings.Select(x => x.Message).Concat(read.Warnings), ManifestJsonSerializerOptions.Default),
                ValidatorVersion = "v1"
            }, cancellationToken);

            runItem.Status = packageVersion.ValidationStatus == ValidationStatus.Valid ? SyncRunItemStatus.Indexed : SyncRunItemStatus.Invalid;
            runItem.PackageVersion = packageVersion;
            runItem.PackageVersionId = packageVersion.Id;
            Increment(counters, runItem.Status == SyncRunItemStatus.Indexed ? "indexed" : "invalid");
        }
        catch (Exception ex)
        {
            runItem.Status = SyncRunItemStatus.Failed;
            runItem.Error = ex.Message;
            diagnostics.SyncItemFailed(run.Id, discovered.PackageId, discovered.Version, ex.Message);
            Increment(counters, "failed");
        }
        finally
        {
            runItem.CompletedAt = DateTimeOffset.UtcNow;
            await catalog.SaveChangesAsync(CancellationToken.None);
        }

        return runItem;
    }

    private async Task<SyncRunItem> AddItemAsync(
        SyncRun run,
        Guid? sourceId,
        string? packageId,
        string? version,
        SyncRunItemStatus status,
        CancellationToken cancellationToken,
        string? error = null)
    {
        var item = new SyncRunItem
        {
            SyncRun = run,
            SyncRunId = run.Id,
            SourceId = sourceId,
            PackageId = packageId,
            Version = version,
            Status = status,
            Error = error
        };
        run.Items.Add(item);
        await syncRuns.AddItemAsync(item, cancellationToken);
        return item;
    }

    private static ValidationStatus ToValidationStatus(ManifestValidationResult validation) =>
        validation.Status == ManifestValidationStatus.UnsupportedSchema
            ? ValidationStatus.UnsupportedSchema
            : validation.IsValid
                ? ValidationStatus.Valid
                : ValidationStatus.Invalid;

    private static string? ExtractSchemaVersion(string manifestJson)
    {
        using var document = JsonDocument.Parse(manifestJson);
        return document.RootElement.TryGetProperty("schemaVersion", out var schemaVersion)
            ? schemaVersion.GetString()
            : null;
    }

    private static void UpdateLatestVersion(Package package, string version)
    {
        if (package.LatestVersion is null || CompareVersions(version, package.LatestVersion) > 0)
            package.LatestVersion = version;
    }

    private static int CompareVersions(string left, string right)
    {
        if (NuGetVersion.TryParse(left, out var leftVersion) && NuGetVersion.TryParse(right, out var rightVersion))
            return leftVersion.CompareTo(rightVersion);

        return StringComparer.OrdinalIgnoreCase.Compare(left, right);
    }

    private static SyncRun RejectedRun(SyncRunTrigger trigger, string error) =>
        new()
        {
            Trigger = trigger,
            Status = SyncRunStatus.Failed,
            CompletedAt = DateTimeOffset.UtcNow,
            Error = error
        };

    private static void Increment(Dictionary<string, int> counters, string name) =>
        counters[name] = counters.GetValueOrDefault(name) + 1;

    private static string BuildSourceFailureDetail(SyncRunItem? firstFailure, int failureCount)
    {
        if (firstFailure is null)
            return "Sync failed.";

        var subject = FormatPackageSubject(firstFailure);
        var detail = string.IsNullOrWhiteSpace(subject)
            ? firstFailure.Error ?? "Sync failed."
            : $"{subject}: {firstFailure.Error ?? "Sync failed."}";

        if (failureCount > 1)
            detail = $"{detail} ({failureCount - 1} more failed)";

        return LimitFailureDetail(detail);
    }

    private static string FormatPackageSubject(SyncRunItem item)
    {
        if (string.IsNullOrWhiteSpace(item.PackageId))
            return "";

        return string.IsNullOrWhiteSpace(item.Version)
            ? item.PackageId
            : $"{item.PackageId} {item.Version}";
    }

    private static string LimitFailureDetail(string detail) =>
        detail.Length <= 2048 ? detail : string.Concat(detail.AsSpan(0, 2045), "...");
}

public interface ISyncCatalogStore
{
    Task<Package?> GetPackageAsync(Guid sourceId, string packageId, CancellationToken cancellationToken = default);
    Task<PackageVersion?> GetPackageVersionAsync(Guid packageId, string version, CancellationToken cancellationToken = default);
    Task AddPackageAsync(Package package, CancellationToken cancellationToken = default);
    Task AddValidationResultAsync(ManifestValidationResultRecord result, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface ISyncRunStore
{
    Task<IReadOnlyList<SyncRun>> ListAsync(CancellationToken cancellationToken = default);
    Task<SyncRun?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(SyncRun run, CancellationToken cancellationToken = default);
    Task AddItemAsync(SyncRunItem item, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

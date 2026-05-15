using Elsa.Catalog.Core.Packaging;
using Elsa.Catalog.Core.Packages;
using Elsa.Catalog.Core.Sources;
using Elsa.Catalog.Core.Sync;
using Elsa.Catalog.Core.Manifests;
using Elsa.Catalog.Testing;
using Elsa.PackageManifests.Validation;
using FluentAssertions;

namespace Elsa.Catalog.Core.Tests;

public sealed class PackageSyncServiceTests
{
    [Fact]
    public async Task Indexes_valid_manifest_and_records_sync_item()
    {
        var source = PublicCatalogSeedData.CreatePackageSource();
        source.ApprovalPolicy = PackageSourceApprovalPolicy.AutoApprove;
        var sources = new InMemorySourceStore([source]);
        var catalog = new InMemorySyncCatalogStore();
        var syncRuns = new InMemorySyncRunStore();
        var manifestJson = new ManifestFixtureBuilder().WithPackage("Elsa.Email", "1.0.0").WithFeature().BuildJson();
        var service = CreateService(sources, catalog, syncRuns, new FakeDiscovery([new("Elsa.Email", "1.0.0")]), new FakeDownloader(manifestJson));

        var run = await service.SyncAllAsync();

        run.Status.Should().Be(SyncRunStatus.Completed);
        run.Items.Should().ContainSingle(x => x.Status == SyncRunItemStatus.Indexed);
        catalog.Packages.Should().ContainSingle(x => x.PackageId == "Elsa.Email");
    }

    [Fact]
    public async Task Marks_changed_manifest_for_existing_version_as_suspicious()
    {
        var source = PublicCatalogSeedData.CreatePackageSource();
        source.ApprovalPolicy = PackageSourceApprovalPolicy.AutoApprove;
        var package = PublicCatalogSeedData.CreatePackage(source);
        var version = PublicCatalogSeedData.AddVersion(package);
        var sources = new InMemorySourceStore([source]);
        var catalog = new InMemorySyncCatalogStore([package]);
        var syncRuns = new InMemorySyncRunStore();
        var changedManifestJson = new ManifestFixtureBuilder().WithPackage("Elsa.Email", "1.0.0").WithFeature("changed").BuildJson();
        var service = CreateService(sources, catalog, syncRuns, new FakeDiscovery([new("Elsa.Email", "1.0.0")]), new FakeDownloader(changedManifestJson));

        var run = await service.SyncAllAsync();

        run.Items.Should().ContainSingle(x => x.Status == SyncRunItemStatus.Suspicious);
        version.SuspiciousChangeDetected.Should().BeTrue();
    }

    [Fact]
    public async Task Updates_latest_version_when_newer_version_is_indexed()
    {
        var source = PublicCatalogSeedData.CreatePackageSource();
        source.ApprovalPolicy = PackageSourceApprovalPolicy.AutoApprove;
        var package = PublicCatalogSeedData.CreatePackage(source);
        PublicCatalogSeedData.AddVersion(package, "1.0.0");
        package.LatestVersion = "1.0.0";
        var sources = new InMemorySourceStore([source]);
        var catalog = new InMemorySyncCatalogStore([package]);
        var syncRuns = new InMemorySyncRunStore();
        var manifestJson = new ManifestFixtureBuilder().WithPackage("Elsa.Email", "2.0.0").WithFeature().BuildJson();
        var service = CreateService(sources, catalog, syncRuns, new FakeDiscovery([new("Elsa.Email", "2.0.0")]), new FakeDownloader(manifestJson));

        var run = await service.SyncAllAsync();

        run.Status.Should().Be(SyncRunStatus.Completed);
        package.LatestVersion.Should().Be("2.0.0");
    }

    [Fact]
    public async Task Rejects_overlapping_all_source_syncs()
    {
        var source = PublicCatalogSeedData.CreatePackageSource();
        var discovery = new GatedDiscovery();
        var service = CreateService(new InMemorySourceStore([source]), new InMemorySyncCatalogStore(), new InMemorySyncRunStore(), discovery, new FakeDownloader("{}"));

        var running = service.SyncAllAsync();
        await discovery.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var rejected = await service.SyncAllAsync();
        discovery.Release.SetResult();
        var completed = await running;

        rejected.Status.Should().Be(SyncRunStatus.Failed);
        rejected.Error.Should().Contain("already active");
        completed.Status.Should().Be(SyncRunStatus.Completed);
    }

    private static PackageSyncService CreateService(
        IPackageSourceStore sources,
        ISyncCatalogStore catalog,
        ISyncRunStore syncRuns,
        IPackageVersionDiscoveryClient discovery,
        IPackageArchiveDownloader downloader) =>
        new(
            sources,
            catalog,
            syncRuns,
            discovery,
            downloader,
            new FakeManifestReader(),
            new ManifestValidator(),
            new ManifestIngestionService(),
            new PackageVersionPolicy(),
            new NoopSyncDiagnostics(),
            new SyncConcurrencyGuard());

    private sealed class InMemorySourceStore(IReadOnlyList<PackageSource> sources) : IPackageSourceStore
    {
        public Task<IReadOnlyList<PackageSource>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult(sources);
        public Task<PackageSource?> GetAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(sources.SingleOrDefault(x => x.Id == id));
        public Task AddAsync(PackageSource source, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InMemorySyncCatalogStore(IReadOnlyList<Package>? packages = null) : ISyncCatalogStore
    {
        public List<Package> Packages { get; } = packages?.ToList() ?? [];

        public Task<Package?> GetPackageAsync(Guid sourceId, string packageId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Packages.SingleOrDefault(x => x.SourceId == sourceId && x.PackageId == packageId));

        public Task<PackageVersion?> GetPackageVersionAsync(Guid packageId, string version, CancellationToken cancellationToken = default) =>
            Task.FromResult(Packages.SelectMany(x => x.Versions).SingleOrDefault(x => x.PackageId == packageId && x.Version == version));

        public Task AddPackageAsync(Package package, CancellationToken cancellationToken = default)
        {
            Packages.Add(package);
            return Task.CompletedTask;
        }

        public Task AddValidationResultAsync(ManifestValidationResultRecord result, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InMemorySyncRunStore : ISyncRunStore
    {
        public List<SyncRun> Runs { get; } = [];
        public Task<IReadOnlyList<SyncRun>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<SyncRun>>(Runs);
        public Task<SyncRun?> GetAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Runs.SingleOrDefault(x => x.Id == id));
        public Task AddAsync(SyncRun run, CancellationToken cancellationToken = default)
        {
            Runs.Add(run);
            return Task.CompletedTask;
        }

        public Task AddItemAsync(SyncRunItem item, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeDiscovery(IReadOnlyList<DiscoveredPackageVersion> versions) : IPackageVersionDiscoveryClient
    {
        public Task<IReadOnlyList<DiscoveredPackageVersion>> FindPackageVersionsAsync(PackageSource source, CancellationToken cancellationToken = default) => Task.FromResult(versions);
    }

    private sealed class GatedDiscovery : IPackageVersionDiscoveryClient
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<IReadOnlyList<DiscoveredPackageVersion>> FindPackageVersionsAsync(PackageSource source, CancellationToken cancellationToken = default)
        {
            Started.TrySetResult();
            await Release.Task.WaitAsync(cancellationToken);
            return [];
        }
    }

    private sealed class FakeDownloader(string manifestJson) : IPackageArchiveDownloader
    {
        public Task<Stream> DownloadPackageAsync(PackageSource source, string packageId, string version, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream>(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(manifestJson)));
    }

    private sealed class FakeManifestReader : IPackageArchiveManifestReader
    {
        public async Task<PackageManifestReadResult> ReadAsync(Stream packageStream, CancellationToken cancellationToken = default)
        {
            using var reader = new StreamReader(packageStream);
            var json = await reader.ReadToEndAsync(cancellationToken);
            var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
            return PackageManifestReadResult.Found("elsa-package.json", json, hash, []);
        }
    }
}

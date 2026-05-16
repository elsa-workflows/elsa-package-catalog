using Elsa.Catalog.Core.Manifests;
using Elsa.Catalog.Core.Packaging;
using Elsa.Catalog.Core.Packages;
using Elsa.Catalog.Core.Sources;
using Elsa.Catalog.Core.Sync;
using Elsa.Catalog.Persistence.EntityFrameworkCore;
using FluentAssertions;
using Elsa.Catalog.Testing;
using Elsa.PackageManifests.Validation;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Catalog.Persistence.EntityFrameworkCore.Tests;

public sealed class SyncPersistenceTests
{
    [Fact]
    public async Task Persists_sync_run_items_for_diagnostics()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlite("Data Source=:memory:", sqlite =>
            {
                sqlite.MigrationsAssembly(CatalogDatabaseServiceCollectionExtensions.SqliteMigrationsAssembly);
            })
            .Options;

        await using var db = new CatalogDbContext(options);
        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();

        var run = new SyncRun { Trigger = SyncRunTrigger.ManualAll };
        run.Items.Add(new SyncRunItem { SyncRun = run, SyncRunId = run.Id, PackageId = "Elsa.Email", Version = "1.0.0", Status = SyncRunItemStatus.Failed, Error = "No manifest" });
        db.SyncRuns.Add(run);
        await db.SaveChangesAsync();

        var stored = await db.SyncRuns.Include(x => x.Items).SingleAsync();

        stored.Items.Should().ContainSingle(x => x.Error == "No manifest");
    }

    [Fact]
    public async Task Initial_migration_creates_catalog_tables()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlite("Data Source=:memory:", sqlite =>
            {
                sqlite.MigrationsAssembly(CatalogDatabaseServiceCollectionExtensions.SqliteMigrationsAssembly);
            })
            .Options;

        await using var db = new CatalogDbContext(options);
        await db.Database.OpenConnectionAsync();
        await db.Database.MigrateAsync();

        db.PackageSources.Add(PublicCatalogSeedData.CreatePackageSource());
        await db.SaveChangesAsync();

        (await db.PackageSources.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Lists_most_recent_sync_runs_before_limiting()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        await using var db = new CatalogDbContext(options);
        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();

        var oldest = DateTimeOffset.UtcNow.AddDays(-101);
        for (var i = 0; i < 101; i++)
            db.SyncRuns.Add(new SyncRun { Trigger = SyncRunTrigger.Scheduled, StartedAt = oldest.AddMinutes(i) });

        var newest = new SyncRun { Trigger = SyncRunTrigger.ManualAll, StartedAt = DateTimeOffset.UtcNow.AddDays(1) };
        db.SyncRuns.Add(newest);
        await db.SaveChangesAsync();

        var runs = await new SyncRunStore(db).ListAsync();

        runs.Should().HaveCount(100);
        runs[0].Id.Should().Be(newest.Id);
        runs.Should().NotContain(x => x.StartedAt == oldest);
    }

    [Fact]
    public async Task Sync_service_persists_new_run_items_without_concurrency_failure()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        await using var db = new CatalogDbContext(options);
        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();

        var source = PublicCatalogSeedData.CreatePackageSource();
        source.ApprovalPolicy = PackageSourceApprovalPolicy.AutoApprove;
        db.PackageSources.Add(source);
        await db.SaveChangesAsync();

        var manifestJson = new ManifestFixtureBuilder()
            .WithPackage("Elsa.Email", "1.0.0")
            .WithFeature()
            .BuildJson();
        var service = new PackageSyncService(
            new PackageSourceStore(db),
            new SyncCatalogStore(db),
            new SyncRunStore(db),
            new FakeDiscovery([new DiscoveredPackageVersion("Elsa.Email", "1.0.0")]),
            new FakeDownloader(manifestJson),
            new FakeManifestReader(),
            new ManifestValidator(),
            new ManifestIngestionService(),
            new PackageVersionPolicy(),
            new NoopSyncDiagnostics(),
            new SyncConcurrencyGuard(),
            new SourceSyncActivityTracker());

        var run = await service.SyncAllAsync();

        run.Status.Should().Be(SyncRunStatus.Completed);
        (await db.SyncRunItems.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Bulk_sync_persists_source_last_synced_timestamp()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        await using var db = new CatalogDbContext(options);
        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();

        var source = PublicCatalogSeedData.CreatePackageSource();
        db.PackageSources.Add(source);
        await db.SaveChangesAsync();

        var service = new PackageSyncService(
            new PackageSourceStore(db),
            new SyncCatalogStore(db),
            new SyncRunStore(db),
            new FakeDiscovery([]),
            new FakeDownloader("{}"),
            new FakeManifestReader(),
            new ManifestValidator(),
            new ManifestIngestionService(),
            new PackageVersionPolicy(),
            new NoopSyncDiagnostics(),
            new SyncConcurrencyGuard(),
            new SourceSyncActivityTracker());

        await service.SyncAllAsync();

        (await db.PackageSources.SingleAsync()).LastSyncedAt.Should().NotBeNull();
    }

    private sealed class FakeDiscovery(IReadOnlyList<DiscoveredPackageVersion> versions) : IPackageVersionDiscoveryClient
    {
        public Task<IReadOnlyList<DiscoveredPackageVersion>> FindPackageVersionsAsync(PackageSource source, CancellationToken cancellationToken = default) => Task.FromResult(versions);
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

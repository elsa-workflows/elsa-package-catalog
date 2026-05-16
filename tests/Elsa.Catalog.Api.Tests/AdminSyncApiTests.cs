using System.Net;
using Elsa.Catalog.Api.Admin.Sync;
using Elsa.Catalog.Api.Authentication;
using Elsa.Catalog.Core.Packaging;
using Elsa.Catalog.Core.Packages;
using Elsa.Catalog.Core.Sync;
using Elsa.Catalog.Persistence.EntityFrameworkCore;
using Elsa.Catalog.Testing;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Catalog.Api.Tests;

public sealed class AdminSyncApiTests
{
    [Fact]
    public async Task Manual_sync_creates_running_sync_run_and_completes_in_background()
    {
        var discovery = new GatedDiscoveryClient();
        await using var app = new CatalogApiTestApplication().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPackageVersionDiscoveryClient>();
                services.AddSingleton<IPackageVersionDiscoveryClient>(discovery);
            });
        });

        await SeedAsync(app, db =>
        {
            db.PackageSources.Add(PublicCatalogSeedData.CreatePackageSource());
            return Task.CompletedTask;
        });

        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthenticationDefaults.HeaderName, "local-dev-key");

        var response = await client.PostAsync("/api/admin/sync", null).WaitAsync(TimeSpan.FromSeconds(5));
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var run = await response.Content.ReadCatalogJsonAsync<AdminSyncRunResponse>();

        run!.Status.Should().Be(SyncRunStatus.Running);

        var runs = await client.GetCatalogJsonAsync<List<AdminSyncRunResponse>>("/api/admin/sync-runs");
        runs.Should().ContainSingle(x => x.Id == run.Id);

        await discovery.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        discovery.Release.SetResult();
        var completed = await WaitForRunStatusAsync(client, run.Id, SyncRunStatus.Completed);
        completed.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Sync_run_list_includes_source_metadata_and_item_count()
    {
        await using var app = new CatalogApiTestApplication();
        var (runId, sourceId) = await SeedSyncRunWithSourceAsync(app);

        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthenticationDefaults.HeaderName, "local-dev-key");

        var runs = await client.GetCatalogJsonAsync<List<AdminSyncRunResponse>>("/api/admin/sync-runs");

        var run = runs.Should().ContainSingle(x => x.Id == runId).Subject;
        run.ItemCount.Should().Be(1);
        run.Sources.Should().ContainSingle().Which.Should().Be(new AdminSyncRunSourceResponse(sourceId, "Elsa Official"));
        run.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Sync_run_details_include_source_metadata_and_item_count()
    {
        await using var app = new CatalogApiTestApplication();
        var (runId, sourceId) = await SeedSyncRunWithSourceAsync(app);

        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthenticationDefaults.HeaderName, "local-dev-key");

        var run = await client.GetCatalogJsonAsync<AdminSyncRunResponse>($"/api/admin/sync-runs/{runId}");

        run!.ItemCount.Should().Be(1);
        run.Sources.Should().ContainSingle().Which.Should().Be(new AdminSyncRunSourceResponse(sourceId, "Elsa Official"));
        run.Items.Should().ContainSingle(x => x.SourceId == sourceId && x.PackageId == "Elsa.Workflows");
    }

    [Fact]
    public async Task Manual_source_sync_response_includes_source_metadata()
    {
        await using var app = new CatalogApiTestApplication().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPackageVersionDiscoveryClient>();
                services.AddScoped<IPackageVersionDiscoveryClient, ThrowingDiscoveryClient>();
            });
        });

        var sourceId = Guid.NewGuid();
        await SeedAsync(app, db =>
        {
            var source = PublicCatalogSeedData.CreatePackageSource();
            source.Id = sourceId;
            source.Name = "Elsa Official";
            db.PackageSources.Add(source);
            return Task.CompletedTask;
        });

        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthenticationDefaults.HeaderName, "local-dev-key");

        var response = await client.PostAsync($"/api/admin/sync/sources/{sourceId}", null);
        var run = await response.Content.ReadCatalogJsonAsync<AdminSyncRunResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        run!.Status.Should().Be(SyncRunStatus.Running);
        run.ItemCount.Should().Be(0);
        run.Sources.Should().ContainSingle().Which.Should().Be(new AdminSyncRunSourceResponse(sourceId, "Elsa Official"));

        var completed = await WaitForRunStatusAsync(client, run.Id, SyncRunStatus.CompletedWithErrors);
        completed.ItemCount.Should().Be(1);
        completed.Sources.Should().ContainSingle().Which.Should().Be(new AdminSyncRunSourceResponse(sourceId, "Elsa Official"));
    }

    private static async Task<AdminSyncRunResponse> WaitForRunStatusAsync(HttpClient client, Guid runId, SyncRunStatus status)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!timeout.IsCancellationRequested)
        {
            var run = await client.GetCatalogJsonAsync<AdminSyncRunResponse>($"/api/admin/sync-runs/{runId}", timeout.Token);
            if (run?.Status == status)
                return run;

            await Task.Delay(50, timeout.Token);
        }

        throw new TimeoutException($"Sync run {runId} did not reach {status}.");
    }

    private static async Task<(Guid RunId, Guid SourceId)> SeedSyncRunWithSourceAsync(CatalogApiTestApplication app)
    {
        var runId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        await app.SeedAsync(db =>
        {
            var source = PublicCatalogSeedData.CreatePackageSource();
            source.Id = sourceId;
            source.Name = "Elsa Official";

            var run = new SyncRun
            {
                Id = runId,
                Trigger = SyncRunTrigger.ManualSource,
                Status = SyncRunStatus.Completed,
                StartedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
                CompletedAt = DateTimeOffset.UtcNow
            };

            run.Items.Add(new SyncRunItem
            {
                SyncRun = run,
                SyncRunId = run.Id,
                SourceId = source.Id,
                PackageId = "Elsa.Workflows",
                Version = "1.0.0",
                Status = SyncRunItemStatus.Indexed
            });

            db.PackageSources.Add(source);
            db.SyncRuns.Add(run);
            return Task.CompletedTask;
        });

        return (runId, sourceId);
    }

    private static async Task SeedAsync(WebApplicationFactory<Program> app, Func<CatalogDbContext, Task> seed)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
        await seed(db);
        await db.SaveChangesAsync();
    }

    private sealed class ThrowingDiscoveryClient : IPackageVersionDiscoveryClient
    {
        public Task<IReadOnlyList<DiscoveredPackageVersion>> FindPackageVersionsAsync(PackageSource source, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Discovery failed.");
    }

    private sealed class GatedDiscoveryClient : IPackageVersionDiscoveryClient
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
}

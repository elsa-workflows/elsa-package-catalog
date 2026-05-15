using System.Net;
using Elsa.Catalog.Api.Admin.Sync;
using Elsa.Catalog.Api.Authentication;
using Elsa.Catalog.Core.Packages;
using Elsa.Catalog.Core.Sync;
using Elsa.Catalog.Testing;
using FluentAssertions;

namespace Elsa.Catalog.Api.Tests;

public sealed class AdminSyncApiTests
{
    [Fact]
    public async Task Manual_sync_creates_completed_sync_run()
    {
        await using var app = new CatalogApiTestApplication();
        await app.SeedAsync(_ => Task.CompletedTask);
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthenticationDefaults.HeaderName, "local-dev-key");

        var response = await client.PostAsync("/api/admin/sync", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var run = await response.Content.ReadCatalogJsonAsync<AdminSyncRunResponse>();

        run!.Status.Should().Be(SyncRunStatus.Completed);

        var runs = await client.GetCatalogJsonAsync<List<AdminSyncRunResponse>>("/api/admin/sync-runs");
        runs.Should().ContainSingle(x => x.Id == run.Id);
    }

    [Fact]
    public async Task Sync_run_list_includes_source_metadata_and_item_count()
    {
        await using var app = new CatalogApiTestApplication();
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

        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthenticationDefaults.HeaderName, "local-dev-key");

        var runs = await client.GetCatalogJsonAsync<List<AdminSyncRunResponse>>("/api/admin/sync-runs");

        var run = runs.Should().ContainSingle(x => x.Id == runId).Subject;
        run.ItemCount.Should().Be(1);
        run.Sources.Should().ContainSingle().Which.Should().Be(new AdminSyncRunSourceResponse(sourceId, "Elsa Official"));
        run.Items.Should().BeEmpty();
    }
}

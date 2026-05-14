using System.Net;
using System.Net.Http.Json;
using Elsa.Catalog.Api.Admin.Sync;
using Elsa.Catalog.Api.Authentication;
using Elsa.Catalog.Core.Packages;
using Elsa.Catalog.Core.Sync;
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
        var run = await response.Content.ReadFromJsonAsync<AdminSyncRunResponse>();

        run!.Status.Should().Be(SyncRunStatus.Completed);

        var runs = await client.GetFromJsonAsync<List<AdminSyncRunResponse>>("/api/admin/sync-runs");
        runs.Should().ContainSingle(x => x.Id == run.Id);
    }
}

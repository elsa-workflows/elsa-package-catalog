using System.Net;
using System.Net.Http.Json;
using Elsa.Catalog.Api.Admin.Sources;
using Elsa.Catalog.Api.Authentication;
using Elsa.Catalog.Core.Packages;
using FluentAssertions;

namespace Elsa.Catalog.Api.Tests;

public sealed class AdminSourcesApiTests
{
    [Fact]
    public async Task Admin_sources_require_api_key()
    {
        await using var app = new CatalogApiTestApplication();

        var response = await app.CreateClient().GetAsync("/api/admin/sources");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Can_create_and_list_source()
    {
        await using var app = new CatalogApiTestApplication();
        await app.SeedAsync(_ => Task.CompletedTask);
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthenticationDefaults.HeaderName, "local-dev-key");

        var create = await client.PostAsJsonAsync("/api/admin/sources", new AdminSourceRequest(
            "NuGet",
            "https://example.test/v3/index.json",
            true,
            ["Elsa.*"],
            ["Elsa.Experimental.*"],
            PackageSourceApprovalPolicy.Manual));

        create.StatusCode.Should().Be(HttpStatusCode.OK);

        var sources = await client.GetFromJsonAsync<List<AdminSourceResponse>>("/api/admin/sources");

        sources.Should().ContainSingle(x =>
            x.Name == "NuGet" &&
            x.IncludePatterns.Contains("Elsa.*") &&
            x.ExcludePatterns.Contains("Elsa.Experimental.*"));
    }

    [Fact]
    public async Task Invalid_source_returns_bad_request()
    {
        await using var app = new CatalogApiTestApplication();
        await app.SeedAsync(_ => Task.CompletedTask);
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthenticationDefaults.HeaderName, "local-dev-key");

        var response = await client.PostAsJsonAsync("/api/admin/sources", new AdminSourceRequest(
            "NuGet",
            "not-a-url",
            true,
            [],
            [],
            PackageSourceApprovalPolicy.Manual));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

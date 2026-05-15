using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Elsa.Catalog.Api.Admin.Sources;
using Elsa.Catalog.Api.Authentication;
using FluentAssertions;

namespace Elsa.Catalog.Api.Tests;

public sealed class AdminDashboardAuthenticationTests
{
    [Fact]
    public async Task Dashboard_route_redirects_anonymous_browser_to_login()
    {
        await using var app = new CatalogApiTestApplication();
        var client = app.CreateClient(new() { AllowAutoRedirect = false });
        using var request = new HttpRequestMessage(HttpMethod.Get, "/admin/overview");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.OriginalString.Should().StartWith("/admin/login");
    }

    [Fact]
    public async Task Dashboard_asset_rejects_anonymous_non_browser_request()
    {
        await using var app = new CatalogApiTestApplication();
        var response = await app.CreateClient(new() { AllowAutoRedirect = false })
            .GetAsync("/admin/assets/index.js");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_page_is_available_anonymously()
    {
        await using var app = new CatalogApiTestApplication();
        var response = await app.CreateClient().GetAsync(AdminDashboardAuthenticationDefaults.LoginPath);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Elsa Catalog Admin");
    }

    [Fact]
    public async Task Login_rejects_invalid_admin_key()
    {
        await using var app = new CatalogApiTestApplication();
        var client = app.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.PostAsync(AdminDashboardAuthenticationDefaults.LoginPath, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["apiKey"] = "wrong",
            ["returnUrl"] = "/admin/overview"
        }));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeFalse();
    }

    [Fact]
    public async Task Login_with_valid_admin_key_authorizes_admin_api_with_cookie()
    {
        await using var app = new CatalogApiTestApplication();
        await app.SeedAsync(_ => Task.CompletedTask);
        var client = app.CreateClient(new() { AllowAutoRedirect = false });

        var login = await client.PostAsync(AdminDashboardAuthenticationDefaults.LoginPath, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["apiKey"] = "local-dev-key",
            ["returnUrl"] = "/admin/overview"
        }));

        login.StatusCode.Should().Be(HttpStatusCode.Redirect);
        login.Headers.Location.Should().Be("/admin/overview");

        var sources = await client.GetAsync("/api/admin/sources");

        sources.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await sources.Content.ReadFromJsonAsync<List<AdminSourceResponse>>();
        payload.Should().NotBeNull();
    }

    [Fact]
    public async Task Logout_clears_dashboard_session()
    {
        await using var app = new CatalogApiTestApplication();
        var client = app.CreateClient(new() { AllowAutoRedirect = false });
        await client.PostAsync(AdminDashboardAuthenticationDefaults.LoginPath, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["apiKey"] = "local-dev-key"
        }));

        var logout = await client.PostAsync(AdminDashboardAuthenticationDefaults.LogoutPath, null);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/admin/overview");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
        var dashboard = await client.SendAsync(request);

        logout.StatusCode.Should().Be(HttpStatusCode.Redirect);
        logout.Headers.Location.Should().Be(AdminDashboardAuthenticationDefaults.LoginPath);
        dashboard.StatusCode.Should().Be(HttpStatusCode.Redirect);
        dashboard.Headers.Location!.OriginalString.Should().StartWith("/admin/login");
    }

    [Fact]
    public async Task Public_endpoint_remains_anonymous()
    {
        await using var app = new CatalogApiTestApplication();
        var response = await app.CreateClient().GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

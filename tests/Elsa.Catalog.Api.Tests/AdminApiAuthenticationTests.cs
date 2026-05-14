using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;

namespace Elsa.Catalog.Api.Tests;

public sealed class AdminApiAuthenticationTests
{
    [Fact]
    public async Task Health_endpoint_is_public()
    {
        await using var app = new WebApplicationFactory<Program>();
        var response = await app.CreateClient().GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

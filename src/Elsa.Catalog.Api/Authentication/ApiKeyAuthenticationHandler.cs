using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Elsa.Catalog.Api.Authentication;

public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyAuthenticationDefaults.HeaderName, out var values))
            return Task.FromResult(AuthenticateResult.NoResult());

        var configuredApiKey = configuration[ApiKeyAuthenticationDefaults.ConfigurationKey];
        if (string.IsNullOrWhiteSpace(configuredApiKey))
            configuredApiKey = "local-dev-key";

        var suppliedApiKey = values.FirstOrDefault();
        if (!string.Equals(configuredApiKey, suppliedApiKey, StringComparison.Ordinal))
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "api-key"), new Claim(ClaimTypes.Name, "api-key")],
            Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}

using Microsoft.AspNetCore.Authorization;

namespace Elsa.Catalog.Api.Authentication;

public static class AdminAuthorization
{
    public const string Policy = "AdminApi";

    public static IServiceCollection AddCatalogAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
            options.AddPolicy(Policy, policy =>
            {
                policy.AuthenticationSchemes.Add(ApiKeyAuthenticationDefaults.Scheme);
                policy.RequireAuthenticatedUser();
            }));
        return services;
    }
}

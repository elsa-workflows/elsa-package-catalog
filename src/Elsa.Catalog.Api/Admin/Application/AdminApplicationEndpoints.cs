using System.Reflection;
using Elsa.Catalog.Api.Authentication;

namespace Elsa.Catalog.Api.Admin.Application;

public static class AdminApplicationEndpoints
{
    public static IEndpointRouteBuilder MapAdminApplicationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/admin/application", () => Results.Ok(GetApplicationInfo()))
            .RequireAuthorization(AdminAuthorization.Policy)
            .WithTags("Admin Application");

        return endpoints;
    }

    private static AdminApplicationResponse GetApplicationInfo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var buildNumber = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        return new AdminApplicationResponse(
            assembly.GetName().Name ?? "Elsa.Catalog.Api",
            string.IsNullOrWhiteSpace(buildNumber) ? assembly.GetName().Version?.ToString() ?? "unknown" : buildNumber);
    }
}

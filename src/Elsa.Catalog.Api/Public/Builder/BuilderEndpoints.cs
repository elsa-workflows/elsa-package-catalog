using Elsa.Catalog.Api.Public.Compatibility;
using Elsa.Catalog.Api.Public.Packages;
using Elsa.Catalog.Core.Builder;
using Elsa.Catalog.Core.Compatibility;
using Elsa.Catalog.Core.Packages;

namespace Elsa.Catalog.Api.Public.Builder;

public static class BuilderEndpoints
{
    public static IEndpointRouteBuilder MapBuilderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/builder").WithTags("Runtime Builder");

        group.MapGet("/catalog", async (PublicCatalogQueryService catalog, InfrastructureProviderCatalog infrastructure, CancellationToken cancellationToken) =>
        {
            var packages = await catalog.ListPackagesAsync(cancellationToken);
            return Results.Ok(new BuilderCatalogResponse(
                packages.Select(PublicPackageEndpoints.ToResponse).ToList(),
                infrastructure.ListProviders().Select(ToResponse).ToList()));
        });

        group.MapGet("/infrastructure/providers", (InfrastructureProviderCatalog infrastructure) =>
            Results.Ok(infrastructure.ListProviders().Select(ToResponse)));

        group.MapPost("/resolve", async (BuilderResolveRequest request, CompatibilityCheckService compatibility, CancellationToken cancellationToken) =>
        {
            var result = await compatibility.CheckAsync(new CompatibilityCheckRequest(
                request.ElsaVersion,
                request.DockerImageVersion,
                request.Packages.Select(x => new SelectedPackageVersion(x.PackageId, x.Version)).ToList(),
                request.Features ?? []), cancellationToken);

            return Results.Ok(new BuilderResolveResponse(
                result.Compatible,
                result.Findings.Select(x => new CompatibilityFindingApiResponse(x.Severity, x.Code, x.Message)).ToList()));
        });

        return endpoints;
    }

    private static BuilderInfrastructureProviderResponse ToResponse(InfrastructureProvider provider) =>
        new(provider.Id, provider.DisplayName, provider.Kind, provider.Strategy, provider.Provider, provider.Capabilities, provider.Outputs);
}

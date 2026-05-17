using Elsa.Catalog.Api.Public.Compatibility;
using Elsa.Catalog.Api.Public.Packages;
using Elsa.Catalog.Core.Builder;
using Elsa.Catalog.Core.Compatibility;
using Elsa.Catalog.Core.Packages;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Catalog.Api.Public.Builder;

public static class BuilderEndpoints
{
    public static IEndpointRouteBuilder MapBuilderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/builder").WithTags("Runtime Builder");

        group.MapGet("/catalog", async ([FromQuery] Guid[] sourceIds, PublicCatalogQueryService catalog, InfrastructureProviderCatalog infrastructure, CancellationToken cancellationToken) =>
        {
            var packages = await catalog.ListPackagesAsync(sourceIds, cancellationToken);
            return Results.Ok(new BuilderCatalogResponse(
                packages.Select(PublicPackageEndpoints.ToResponse).ToList(),
                infrastructure.ListProviders().Select(ToResponse).ToList()));
        });

        group.MapGet("/infrastructure/providers", (InfrastructureProviderCatalog infrastructure) =>
            Results.Ok(infrastructure.ListProviders().Select(ToResponse)));

        group.MapPost("/resolve", async (BuilderResolveRequest request, CompatibilityCheckService compatibility, CancellationToken cancellationToken) =>
        {
            if (request.Packages is null)
                return Results.BadRequest(new { error = "packages is required." });

            var features = request.Features ?? request.Packages
                .SelectMany(x => x.SelectedFeatures ?? [])
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var result = await compatibility.CheckAsync(new CompatibilityCheckRequest(
                request.ElsaVersion,
                request.DockerImageVersion,
                request.Packages.Select(x => new SelectedPackageVersion(x.SourceId, x.PackageId, x.Version)).ToList(),
                features), cancellationToken);

            return Results.Ok(new BuilderResolveResponse(
                result.Compatible,
                result.Findings.Select(x => new CompatibilityFindingApiResponse(x.Severity, x.Code, x.Message)).ToList()));
        });

        return endpoints;
    }

    private static BuilderInfrastructureProviderResponse ToResponse(InfrastructureProvider provider) =>
        new(provider.Id, provider.DisplayName, provider.Kind, provider.Strategy, provider.Provider, provider.Capabilities, provider.Outputs);
}

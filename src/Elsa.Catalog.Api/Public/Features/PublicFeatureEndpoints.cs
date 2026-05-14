using Elsa.Catalog.Core.Packages;

namespace Elsa.Catalog.Api.Public.Features;

public static class PublicFeatureEndpoints
{
    public static IEndpointRouteBuilder MapPublicFeatureEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/features").WithTags("Public Features");

        group.MapGet("/", async (PublicCatalogQueryService catalog, CancellationToken cancellationToken) =>
        {
            var features = await catalog.ListFeaturesAsync(cancellationToken);
            return Results.Ok(features.Select(ToResponse));
        });

        group.MapGet("/{featureId}", async (string featureId, PublicCatalogQueryService catalog, CancellationToken cancellationToken) =>
        {
            var feature = await catalog.GetFeatureAsync(featureId, cancellationToken);
            return feature is null ? Results.NotFound() : Results.Ok(ToResponse(feature));
        });

        return endpoints;
    }

    private static PublicFeatureResponse ToResponse(PublicFeatureProjection feature) =>
        new(
            feature.FeatureId,
            feature.PackageId,
            feature.PackageVersion,
            feature.TypeName,
            feature.DisplayName,
            feature.Description,
            feature.Category,
            feature.Advanced,
            feature.Experimental,
            feature.Settings.Select(ToResponse).ToList());

    private static PublicFeatureSettingResponse ToResponse(PublicFeatureSettingProjection setting) =>
        new(
            setting.Name,
            setting.ClrType,
            setting.JsonType,
            setting.Required,
            setting.DisplayName,
            setting.Description,
            setting.Category,
            setting.Secret,
            setting.RestartRequired,
            setting.EnvironmentVariable);
}

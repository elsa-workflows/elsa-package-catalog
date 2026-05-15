using Elsa.Catalog.Core.Packages;

namespace Elsa.Catalog.Api.Public.Packages;

public static class PublicPackageEndpoints
{
    public static IEndpointRouteBuilder MapPublicPackageEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/packages").WithTags("Public Packages");

        group.MapGet("/", async (PublicCatalogQueryService catalog, CancellationToken cancellationToken) =>
        {
            var packages = await catalog.ListPackagesAsync(cancellationToken);
            return Results.Ok(packages.Select(ToResponse));
        });

        group.MapGet("/{packageId}", async (string packageId, PublicCatalogQueryService catalog, CancellationToken cancellationToken) =>
        {
            var package = await catalog.GetPackageAsync(packageId, cancellationToken);
            return package is null ? Results.NotFound() : Results.Ok(ToResponse(package));
        });

        group.MapGet("/{packageId}/versions", async (string packageId, PublicCatalogQueryService catalog, CancellationToken cancellationToken) =>
        {
            var versions = await catalog.ListVersionsAsync(packageId, cancellationToken);
            return Results.Ok(versions.Select(ToResponse));
        });

        group.MapGet("/{packageId}/versions/{version}", async (string packageId, string version, PublicCatalogQueryService catalog, CancellationToken cancellationToken) =>
        {
            var packageVersion = await catalog.GetVersionAsync(packageId, version, cancellationToken);
            return packageVersion is null ? Results.NotFound() : Results.Ok(ToResponse(packageVersion));
        });

        return endpoints;
    }

    private static PublicPackageResponse ToResponse(PublicPackageProjection package) =>
        new(package.PackageId, package.LatestVersion, package.Versions.Select(ToResponse).ToList());

    private static PublicPackageVersionResponse ToResponse(PublicPackageVersionProjection version) =>
        new(version.PackageId, version.Version, version.SchemaVersion, version.PublishedAt, version.Features.Select(ToResponse).ToList());

    private static PublicPackageFeatureResponse ToResponse(PublicFeatureProjection feature) =>
        new(
            feature.FeatureId,
            feature.TypeName,
            feature.DisplayName,
            feature.Description,
            feature.Category,
            feature.Advanced,
            feature.Experimental,
            feature.Settings.Select(ToResponse).ToList());

    private static PublicPackageFeatureSettingResponse ToResponse(PublicFeatureSettingProjection setting) =>
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

using System.Text.Json;
using Elsa.Catalog.Core.Packages;
using Elsa.PackageManifests;

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

    public static PublicPackageResponse ToResponse(PublicPackageProjection package) =>
        new(package.PackageId, ToResponse(package.Source), package.LatestVersion, package.Versions.Select(ToResponse).ToList());

    public static PublicPackageVersionResponse ToResponse(PublicPackageVersionProjection version) =>
        new(version.PackageId, version.Version, ToResponse(version.Source), version.SchemaVersion, version.PublishedAt, version.Features.Select(ToResponse).ToList());

    public static PublicPackageSourceResponse ToResponse(PublicPackageSourceProjection source) =>
        new(source.Id, source.Name, source.Url);

    public static PublicPackageFeatureResponse ToResponse(PublicFeatureProjection feature) =>
        new(
            feature.FeatureId,
            feature.TypeName,
            feature.DisplayName,
            feature.Description,
            feature.Category,
            feature.RequiredCapabilities,
            feature.Dependencies.Select(x => new PublicPackageDependencyResponse(x.PackageId, x.VersionRange, x.FeatureId, x.Optional, x.Reason)).ToList(),
            feature.Conflicts.Select(x => new PublicPackageConflictResponse(x.PackageId, x.VersionRange, x.FeatureId, x.Reason)).ToList(),
            feature.Infrastructure.Select(x => new PublicPackageInfrastructureRequirementResponse(x.Id, x.Kind, x.Optional, x.Reason, x.Capabilities, x.Providers, x.ConfigurationKeys, ParseObject(x.ExtensionsJson))).ToList(),
            feature.Advanced,
            feature.Experimental,
            ParseObject(feature.ExtensionsJson),
            feature.Settings.Select(ToResponse).ToList());

    public static PublicPackageFeatureSettingResponse ToResponse(PublicFeatureSettingProjection setting) =>
        new(
            setting.Name,
            setting.ClrType,
            setting.JsonType,
            setting.Required,
            ParseValue(setting.DefaultValueJson),
            setting.DisplayName,
            setting.Description,
            setting.Category,
            ParseObject(setting.ValidationJson),
            setting.Secret,
            setting.RestartRequired,
            setting.EnvironmentVariable,
            ParseObject(setting.UiJson),
            ParseObject(setting.ExtensionsJson));

    private static JsonElement? ParseValue(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static IReadOnlyDictionary<string, JsonElement> ParseObject(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<string, JsonElement>();

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, ManifestJsonSerializerOptions.Default) ?? new Dictionary<string, JsonElement>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, JsonElement>();
        }
    }
}

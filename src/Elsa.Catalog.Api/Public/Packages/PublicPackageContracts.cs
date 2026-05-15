namespace Elsa.Catalog.Api.Public.Packages;

public sealed record PublicPackageResponse(
    string PackageId,
    string? LatestVersion,
    IReadOnlyList<PublicPackageVersionResponse> Versions);

public sealed record PublicPackageVersionResponse(
    string PackageId,
    string Version,
    string? SchemaVersion,
    DateTimeOffset? PublishedAt,
    IReadOnlyList<PublicPackageFeatureResponse> Features);

public sealed record PublicPackageFeatureResponse(
    string FeatureId,
    string TypeName,
    string DisplayName,
    string? Description,
    string? Category,
    bool Advanced,
    bool Experimental,
    IReadOnlyList<PublicPackageFeatureSettingResponse> Settings);

public sealed record PublicPackageFeatureSettingResponse(
    string Name,
    string? ClrType,
    string JsonType,
    bool Required,
    string DisplayName,
    string? Description,
    string? Category,
    bool Secret,
    bool RestartRequired,
    string? EnvironmentVariable);

namespace Elsa.Catalog.Api.Public.Features;

public sealed record PublicFeatureResponse(
    string FeatureId,
    string PackageId,
    string PackageVersion,
    string TypeName,
    string DisplayName,
    string? Description,
    string? Category,
    bool Advanced,
    bool Experimental,
    IReadOnlyList<PublicFeatureSettingResponse> Settings);

public sealed record PublicFeatureSettingResponse(
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

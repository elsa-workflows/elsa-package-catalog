namespace Elsa.Catalog.Api.Public.Features;

public sealed record PublicFeatureResponse(
    string FeatureId,
    string PackageId,
    string PackageVersion,
    PublicFeatureSourceResponse Source,
    string TypeName,
    string DisplayName,
    string? Description,
    string? Category,
    IReadOnlyList<string> RequiredCapabilities,
    IReadOnlyList<PublicFeatureDependencyResponse> Dependencies,
    IReadOnlyList<PublicFeatureConflictResponse> Conflicts,
    IReadOnlyList<PublicFeatureInfrastructureRequirementResponse> Infrastructure,
    bool Advanced,
    bool Experimental,
    string ExtensionsJson,
    IReadOnlyList<PublicFeatureSettingResponse> Settings);

public sealed record PublicFeatureSourceResponse(
    Guid Id,
    string Name,
    string Url);

public sealed record PublicFeatureSettingResponse(
    string Name,
    string? ClrType,
    string JsonType,
    bool Required,
    string? DefaultValueJson,
    string DisplayName,
    string? Description,
    string? Category,
    string ValidationJson,
    bool Secret,
    bool RestartRequired,
    string? EnvironmentVariable,
    string UiJson,
    string ExtensionsJson);

public sealed record PublicFeatureDependencyResponse(
    string? PackageId,
    string? VersionRange,
    string? FeatureId,
    bool Optional,
    string? Reason);

public sealed record PublicFeatureConflictResponse(
    string? PackageId,
    string? VersionRange,
    string? FeatureId,
    string? Reason);

public sealed record PublicFeatureInfrastructureRequirementResponse(
    string Id,
    string Kind,
    bool Optional,
    string? Reason,
    IReadOnlyList<string> Capabilities,
    IReadOnlyList<string> Providers,
    IReadOnlyList<string> ConfigurationKeys,
    string ExtensionsJson);

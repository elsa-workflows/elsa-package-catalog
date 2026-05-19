using Elsa.Catalog.Api.Public.Packages;
using Elsa.Catalog.Api.Public.Compatibility;
using System.Text.Json;

namespace Elsa.Catalog.Api.Public.Builder;

public sealed record BuilderCatalogResponse(
    IReadOnlyList<PublicPackageResponse> Packages,
    IReadOnlyList<BuilderInfrastructureProviderResponse> InfrastructureProviders);

public sealed record BuilderInfrastructureProviderResponse(
    string Id,
    string DisplayName,
    string Kind,
    string Strategy,
    string Provider,
    IReadOnlyList<string> Capabilities,
    IReadOnlyList<string> Outputs);

public sealed record BuilderResolveRequest(
    string? ElsaVersion,
    string? DockerImageVersion,
    IReadOnlyList<BuilderSelectedPackageRequest>? Packages,
    IReadOnlyList<string>? Features);

public sealed record BuilderSelectedPackageRequest(
    Guid SourceId,
    string PackageId,
    string Version,
    IReadOnlyList<string>? SelectedFeatures);

public sealed record BuilderResolveResponse(
    bool Compatible,
    IReadOnlyList<CompatibilityFindingApiResponse> Findings);

public sealed record BuilderBundleRequest(
    BuilderBundleImageRequest? Image,
    IReadOnlyList<BuilderBundlePackageRequest>? Packages,
    IReadOnlyList<BuilderBundlePackageSourceRequest>? PackageSources,
    IReadOnlyList<BuilderBundleInfrastructureRequest>? Infrastructure,
    BuilderBundleLocalPackagesRequest? LocalPackages);

public sealed record BuilderBundleImageRequest(
    string? Slug,
    string? Tag,
    int? HostPort,
    IReadOnlyDictionary<string, string>? EnvOverrides);

public sealed record BuilderBundlePackageRequest(
    Guid SourceId,
    string? PackageId,
    string? Version,
    IReadOnlyList<string>? SelectedFeatures,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, JsonElement>>? Settings);

public sealed record BuilderBundlePackageSourceRequest(
    Guid SourceId,
    string? Name,
    string? Url,
    string? Kind);

public sealed record BuilderBundleInfrastructureRequest(
    string? Kind,
    string? ProviderId,
    string? Strategy,
    IReadOnlyDictionary<string, JsonElement>? Settings);

public sealed record BuilderBundleLocalPackagesRequest(
    bool Enabled,
    string? DirectoryPath);

public sealed record BuilderBundleResponse(
    string BundleId,
    IReadOnlyList<BuilderBundleFileResponse> Files,
    IReadOnlyList<BuilderBundleFindingResponse> Findings);

public sealed record BuilderBundleFileResponse(
    string Path,
    string Language,
    string ContentType,
    bool Required,
    string Contents);

public sealed record BuilderBundleFindingResponse(
    string Level,
    string Code,
    string Message,
    string? Scope);

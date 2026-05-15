using Elsa.Catalog.Api.Public.Packages;
using Elsa.Catalog.Api.Public.Compatibility;

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
    IReadOnlyList<BuilderSelectedPackageRequest> Packages,
    IReadOnlyList<string>? Features);

public sealed record BuilderSelectedPackageRequest(
    string PackageId,
    string Version);

public sealed record BuilderResolveResponse(
    bool Compatible,
    IReadOnlyList<CompatibilityFindingApiResponse> Findings);

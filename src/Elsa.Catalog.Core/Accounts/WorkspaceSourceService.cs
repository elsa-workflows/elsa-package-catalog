using Elsa.Catalog.Core.Packages;
using Elsa.Catalog.Core.Sources;

namespace Elsa.Catalog.Core.Accounts;

public sealed class WorkspaceSourceService(IAccountWorkspaceStore store, PackageSourceValidator validator, IPublicCatalogCacheInvalidator? publicCatalogCache = null)
{
    public async Task<WorkspaceSourceResult> CreateSourceAsync(WorkspaceAccess access, WorkspaceSourceCreateRequest request, CancellationToken cancellationToken = default)
    {
        if (!access.CanAdministerSources)
            return WorkspaceSourceResult.Forbidden("Workspace source administrator role is required.");

        var entitlement = await store.GetLatestEntitlementAsync(access.WorkspaceId, cancellationToken);
        if (entitlement is null || !entitlement.CanCreateCustomSources)
            return WorkspaceSourceResult.Forbidden("Workspace is not entitled to create custom sources.");

        var currentSourceCount = await store.CountActiveWorkspaceSourcesAsync(access.WorkspaceId, cancellationToken);
        if (currentSourceCount >= entitlement.MaxSources)
            return WorkspaceSourceResult.Forbidden("Workspace custom source limit has been reached.");

        if (await store.WorkspaceSourceUrlExistsAsync(access.WorkspaceId, request.Url, cancellationToken))
            return WorkspaceSourceResult.Invalid(["A source with this URL already exists in the workspace."]);

        if (Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) && !string.IsNullOrWhiteSpace(uri.UserInfo))
            return WorkspaceSourceResult.Invalid(["Private feed credentials in source URLs are not supported yet."]);

        var source = new PackageSource
        {
            Name = request.Name,
            Url = request.Url,
            Enabled = request.Enabled,
            Browseable = true,
            Visibility = PackageSourceVisibility.Workspace,
            OwnerWorkspaceId = access.WorkspaceId,
            IncludePatterns = request.IncludePatterns.ToList(),
            ExcludePatterns = request.ExcludePatterns.ToList(),
            ApprovalPolicy = PackageSourceApprovalPolicy.Manual,
            VersionDiscoveryPolicy = request.VersionDiscoveryPolicy
        };
        var validation = validator.Validate(source);
        if (!validation.IsValid)
            return WorkspaceSourceResult.Invalid(validation.Errors);

        source.CreatedAt = DateTimeOffset.UtcNow;
        source.UpdatedAt = source.CreatedAt;
        await store.AddWorkspaceSourceAsync(source, cancellationToken);
        await store.SaveChangesAsync(cancellationToken);
        publicCatalogCache?.Invalidate();
        return WorkspaceSourceResult.Success(source);
    }
}

public sealed record WorkspaceSourceCreateRequest(
    string Name,
    string Url,
    bool Enabled,
    IReadOnlyList<string> IncludePatterns,
    IReadOnlyList<string> ExcludePatterns,
    PackageSourceVersionDiscoveryPolicy VersionDiscoveryPolicy);

public sealed record WorkspaceSourceResult(PackageSource? Source, IReadOnlyList<string> Errors, bool ForbiddenResult)
{
    public bool Succeeded => Source is not null && Errors.Count == 0 && !ForbiddenResult;
    public static WorkspaceSourceResult Success(PackageSource source) => new(source, [], false);
    public static WorkspaceSourceResult Invalid(IReadOnlyList<string> errors) => new(null, errors, false);
    public static WorkspaceSourceResult Forbidden(string error) => new(null, [error], true);
}

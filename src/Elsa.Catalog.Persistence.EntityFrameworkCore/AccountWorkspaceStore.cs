using Elsa.Catalog.Core.Accounts;
using Elsa.Catalog.Core.Packages;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Catalog.Persistence.EntityFrameworkCore;

public sealed class AccountWorkspaceStore(CatalogDbContext dbContext) : IAccountWorkspaceStore
{
    public async Task<ExternalIdentityLookup?> FindByExternalIdentityAsync(string issuer, string subject, CancellationToken cancellationToken = default)
    {
        var identity = await dbContext.ExternalIdentities
            .AsNoTracking()
            .Include(x => x.Account)
            .ThenInclude(x => x!.Memberships)
            .ThenInclude(x => x.Workspace)
            .SingleOrDefaultAsync(x => x.Issuer == issuer && x.Subject == subject, cancellationToken);

        if (identity?.Account is null)
            return null;

        return new ExternalIdentityLookup(
            identity.Id,
            new AccountWorkspaceContext(
                new AccountSummary(identity.Account.Id, identity.Account.DisplayName, identity.Account.Email),
                identity.Account.Memberships
                    .Where(x => x.Workspace is { SoftDeletedAt: null })
                    .Select(x => new WorkspaceSummary(x.Workspace!.Id, x.Workspace.Name, x.Workspace.Kind, x.Role))
                    .ToList()));
    }

    public async Task AddAccountAsync(Account account, CancellationToken cancellationToken = default) =>
        await dbContext.Accounts.AddAsync(account, cancellationToken);

    public async Task UpdateExternalIdentitySeenAsync(Guid externalIdentityId, string? displayName, string? email, CancellationToken cancellationToken = default)
    {
        var identity = await dbContext.ExternalIdentities
            .Include(x => x.Account)
            .SingleAsync(x => x.Id == externalIdentityId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        identity.DisplayName = displayName;
        identity.Email = email;
        identity.LastSeenAt = now;
        identity.UpdatedAt = now;
        if (identity.Account is not null)
        {
            identity.Account.DisplayName = displayName;
            identity.Account.Email = email;
            identity.Account.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<WorkspaceEntitlementSnapshot?> GetLatestEntitlementAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        var snapshots = await dbContext.WorkspaceEntitlementSnapshots
            .AsNoTracking()
            .Where(x => x.WorkspaceId == workspaceId)
            .ToListAsync(cancellationToken);

        return snapshots
            .OrderByDescending(x => x.SyncedAt)
            .ThenByDescending(x => x.CreatedAt)
            .FirstOrDefault();
    }

    public async Task<WorkspaceEntitlementSnapshot> SaveEntitlementAsync(WorkspaceEntitlementSnapshot entitlement, CancellationToken cancellationToken = default)
    {
        entitlement.SyncedAt = DateTimeOffset.UtcNow;
        entitlement.CreatedAt = entitlement.SyncedAt;
        entitlement.UpdatedAt = entitlement.SyncedAt;
        await dbContext.WorkspaceEntitlementSnapshots.AddAsync(entitlement, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entitlement;
    }

    public Task<int> CountActiveWorkspaceSourcesAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
        dbContext.PackageSources.CountAsync(x =>
            x.OwnerWorkspaceId == workspaceId &&
            x.Visibility == PackageSourceVisibility.Workspace &&
            x.SoftDeletedAt == null,
            cancellationToken);

    public Task<bool> WorkspaceSourceUrlExistsAsync(Guid workspaceId, string url, CancellationToken cancellationToken = default) =>
        dbContext.PackageSources.AnyAsync(x =>
            x.OwnerWorkspaceId == workspaceId &&
            x.Visibility == PackageSourceVisibility.Workspace &&
            x.SoftDeletedAt == null &&
            x.Url == url,
            cancellationToken);

    public async Task AddWorkspaceSourceAsync(PackageSource source, CancellationToken cancellationToken = default) =>
        await dbContext.PackageSources.AddAsync(source, cancellationToken);

    public async Task<IReadOnlyList<PackageSource>> ListVisibleSourcesAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
        await dbContext.PackageSources
            .AsNoTracking()
            .Where(x => x.Enabled && x.Browseable && x.SoftDeletedAt == null)
            .Where(x =>
                (x.Visibility == PackageSourceVisibility.Public && x.OwnerWorkspaceId == null) ||
                (x.Visibility == PackageSourceVisibility.Workspace && x.OwnerWorkspaceId == workspaceId))
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, int>> GetPackageCountsAsync(IReadOnlyCollection<Guid> sourceIds, CancellationToken cancellationToken = default) =>
        await dbContext.Packages
            .AsNoTracking()
            .Where(x => sourceIds.Contains(x.SourceId))
            .GroupBy(x => x.SourceId)
            .Select(x => new { SourceId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.SourceId, x => x.Count, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}

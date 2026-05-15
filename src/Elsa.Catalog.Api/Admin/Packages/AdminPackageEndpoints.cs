using Elsa.Catalog.Api.Authentication;
using Elsa.Catalog.Core.Approvals;
using Elsa.Catalog.Core.Packages;

namespace Elsa.Catalog.Api.Admin.Packages;

public static class AdminPackageEndpoints
{
    public static IEndpointRouteBuilder MapAdminPackageEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/admin/packages")
            .RequireAuthorization(AdminAuthorization.Policy)
            .WithTags("Admin Packages");

        group.MapGet("/", async (ApprovalService approvals, CancellationToken cancellationToken) =>
            Results.Ok((await approvals.ListPackagesAsync(cancellationToken)).Select(ToResponse)));

        group.MapGet("/{packageId}", async (string packageId, ApprovalService approvals, CancellationToken cancellationToken) =>
        {
            var package = await approvals.GetPackageAsync(packageId, cancellationToken);
            return package is null ? Results.NotFound() : Results.Ok(ToResponse(package));
        });

        return endpoints;
    }

    internal static AdminPackageResponse ToResponse(Package package) =>
        new(
            package.PackageId,
            package.Approved,
            package.Listed,
            package.LatestVersion,
            package.Versions.Select(version => new AdminPackageVersionResponse(
                version.Version,
                version.ValidationStatus,
                version.ApprovalStatus,
                version.IsListed,
                version.SuspiciousChangeDetected,
                version.SchemaVersion)).ToList());
}

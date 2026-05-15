using Elsa.Catalog.Api.Authentication;
using Elsa.Catalog.Core.Approvals;
using Elsa.Catalog.Core.Packages;

namespace Elsa.Catalog.Api.Admin.Packages;

public static class AdminApprovalEndpoints
{
    public static IEndpointRouteBuilder MapAdminApprovalEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/admin/packages")
            .RequireAuthorization(AdminAuthorization.Policy)
            .WithTags("Admin Approval");

        group.MapPost("/{packageId}/approve", async (string packageId, ApprovalRequest? request, ApprovalService approvals, CancellationToken cancellationToken) =>
            await approvals.SetPackageApprovalAsync(packageId, PackageApprovalStatus.Approved, "api-key", request?.Reason, cancellationToken) ? Results.NoContent() : Results.NotFound());

        group.MapPost("/{packageId}/reject", async (string packageId, ApprovalRequest? request, ApprovalService approvals, CancellationToken cancellationToken) =>
            await approvals.SetPackageApprovalAsync(packageId, PackageApprovalStatus.Rejected, "api-key", request?.Reason, cancellationToken) ? Results.NoContent() : Results.NotFound());

        group.MapPost("/{packageId}/versions/{version}/approve", async (string packageId, string version, ApprovalRequest? request, ApprovalService approvals, CancellationToken cancellationToken) =>
            await approvals.SetVersionApprovalAsync(packageId, version, PackageApprovalStatus.Approved, "api-key", request?.Reason, cancellationToken) ? Results.NoContent() : Results.NotFound());

        group.MapPost("/{packageId}/versions/{version}/reject", async (string packageId, string version, ApprovalRequest? request, ApprovalService approvals, CancellationToken cancellationToken) =>
            await approvals.SetVersionApprovalAsync(packageId, version, PackageApprovalStatus.Rejected, "api-key", request?.Reason, cancellationToken) ? Results.NoContent() : Results.NotFound());

        return endpoints;
    }
}

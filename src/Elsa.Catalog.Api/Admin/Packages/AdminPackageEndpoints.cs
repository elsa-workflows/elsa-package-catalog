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

    internal static AdminPackageResponse ToResponse(Package package)
    {
        var latestVersion = package.Versions.FirstOrDefault(version => version.Version == package.LatestVersion) ?? package.Versions.FirstOrDefault();

        return new(
            package.PackageId,
            package.Approved,
            package.Listed,
            package.SourceId,
            package.LatestVersion,
            ToApprovalStatus(package),
            ToValidationStatus(package),
            latestVersion?.Features.Count ?? 0,
            package.UpdatedAt,
            package.Versions.Select(version => new AdminPackageVersionResponse(
                version.Version,
                version.ValidationStatus,
                version.ApprovalStatus,
                version.IsListed,
                version.SuspiciousChangeDetected,
                version.SchemaVersion)).ToList());
    }

    private static PackageApprovalStatus ToApprovalStatus(Package package)
    {
        if (package.Versions.Any(version => version.ApprovalStatus == PackageApprovalStatus.Pending))
            return PackageApprovalStatus.Pending;
        if (package.Versions.Any(version => version.ApprovalStatus == PackageApprovalStatus.Rejected))
            return PackageApprovalStatus.Rejected;

        return package.Approved ? PackageApprovalStatus.Approved : PackageApprovalStatus.Pending;
    }

    private static ValidationStatus ToValidationStatus(Package package)
    {
        if (package.Versions.Any(version => version.ValidationStatus == ValidationStatus.Invalid))
            return ValidationStatus.Invalid;
        if (package.Versions.Any(version => version.ValidationStatus == ValidationStatus.UnsupportedSchema))
            return ValidationStatus.UnsupportedSchema;
        if (package.Versions.Any(version => version.ValidationStatus == ValidationStatus.NotValidated))
            return ValidationStatus.NotValidated;

        return ValidationStatus.Valid;
    }
}

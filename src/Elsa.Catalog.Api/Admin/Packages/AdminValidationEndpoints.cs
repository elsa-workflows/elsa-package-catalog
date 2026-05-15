using Elsa.Catalog.Api.Authentication;
using Elsa.Catalog.Core.Approvals;

namespace Elsa.Catalog.Api.Admin.Packages;

public static class AdminValidationEndpoints
{
    public static IEndpointRouteBuilder MapAdminValidationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/admin/packages/{packageId}/versions/{version}/validation", async (string packageId, string version, IApprovalStore store, CancellationToken cancellationToken) =>
        {
            var results = await store.GetValidationResultsAsync(packageId, version, cancellationToken);
            return Results.Ok(results.Select(x => new AdminValidationResultResponse(x.Id, x.SchemaVersion, x.Status, x.ErrorsJson, x.WarningsJson, x.ValidatedAt, x.ValidatorVersion)));
        })
        .RequireAuthorization(AdminAuthorization.Policy)
        .WithTags("Admin Validation");

        return endpoints;
    }
}

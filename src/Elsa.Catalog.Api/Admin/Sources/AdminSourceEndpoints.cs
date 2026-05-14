using Elsa.Catalog.Api.Authentication;
using Elsa.Catalog.Core.Packages;
using Elsa.Catalog.Core.Sources;

namespace Elsa.Catalog.Api.Admin.Sources;

public static class AdminSourceEndpoints
{
    public static IEndpointRouteBuilder MapAdminSourceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/admin/sources")
            .RequireAuthorization(AdminAuthorization.Policy)
            .WithTags("Admin Sources");

        group.MapGet("/", async (PackageSourceService sources, CancellationToken cancellationToken) =>
            Results.Ok((await sources.ListAsync(cancellationToken)).Select(ToResponse)));

        group.MapPost("/", async (AdminSourceRequest request, PackageSourceService sources, CancellationToken cancellationToken) =>
        {
            var result = await sources.CreateAsync(ToSource(request), cancellationToken);
            return ToResult(result);
        });

        group.MapPut("/{id:guid}", async (Guid id, AdminSourceRequest request, PackageSourceService sources, CancellationToken cancellationToken) =>
        {
            var result = await sources.UpdateAsync(id, ToSource(request), cancellationToken);
            return ToResult(result);
        });

        group.MapDelete("/{id:guid}", async (Guid id, PackageSourceService sources, CancellationToken cancellationToken) =>
            await sources.DeleteAsync(id, cancellationToken) ? Results.NoContent() : Results.NotFound());

        return endpoints;
    }

    private static IResult ToResult(PackageSourceResult result)
    {
        if (result.NotFoundResult)
            return Results.NotFound();

        if (!result.Succeeded)
            return Results.BadRequest(new AdminValidationErrorResponse(result.Errors));

        return Results.Ok(ToResponse(result.Source!));
    }

    private static PackageSource ToSource(AdminSourceRequest request) => new()
    {
        Name = request.Name,
        Type = PackageSourceType.NuGetFeed,
        Url = request.Url,
        Enabled = request.Enabled,
        IncludePatterns = request.IncludePatterns.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList(),
        ExcludePatterns = request.ExcludePatterns?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList() ?? [],
        ApprovalPolicy = request.ApprovalPolicy
    };

    private static AdminSourceResponse ToResponse(PackageSource source) =>
        new(
            source.Id,
            source.Name,
            source.Type,
            source.Url,
            source.Enabled,
            source.IncludePatterns,
            source.ExcludePatterns,
            source.ApprovalPolicy,
            source.LastSyncedAt,
            source.CreatedAt,
            source.UpdatedAt);
}

using Elsa.Catalog.Api.Authentication;
using Elsa.Catalog.Core.Sync;

namespace Elsa.Catalog.Api.Admin.Sync;

public static class AdminSyncEndpoints
{
    public static IEndpointRouteBuilder MapAdminSyncEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/admin")
            .RequireAuthorization(AdminAuthorization.Policy)
            .WithTags("Admin Sync");

        group.MapPost("/sync", async (PackageSyncService sync, CancellationToken cancellationToken) =>
            Results.Ok(ToResponse(await sync.SyncAllAsync(cancellationToken))));

        group.MapPost("/sync/sources/{sourceId:guid}", async (Guid sourceId, PackageSyncService sync, CancellationToken cancellationToken) =>
            Results.Ok(ToResponse(await sync.SyncSourceAsync(sourceId, cancellationToken))));

        group.MapPost("/sync/packages/{packageId}", (string packageId) =>
            Results.BadRequest(new { error = "Manual package sync is not available until package source ownership is known." }));

        group.MapGet("/sync-runs", async (ISyncRunStore syncRuns, CancellationToken cancellationToken) =>
            Results.Ok((await syncRuns.ListAsync(cancellationToken)).Select(ToResponse)));

        group.MapGet("/sync-runs/{id:guid}", async (Guid id, ISyncRunStore syncRuns, CancellationToken cancellationToken) =>
        {
            var run = await syncRuns.GetAsync(id, cancellationToken);
            return run is null ? Results.NotFound() : Results.Ok(ToResponse(run));
        });

        return endpoints;
    }

    private static AdminSyncRunResponse ToResponse(SyncRun run) =>
        new(
            run.Id,
            run.Trigger,
            run.Status,
            run.StartedAt,
            run.CompletedAt,
            run.Error,
            run.SummaryCountersJson,
            run.Items.Select(ToResponse).ToList());

    private static AdminSyncRunItemResponse ToResponse(SyncRunItem item) =>
        new(
            item.Id,
            item.SourceId,
            item.PackageId,
            item.Version,
            item.Status,
            item.Message,
            item.Error,
            item.StartedAt,
            item.CompletedAt);
}

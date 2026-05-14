using Elsa.Catalog.Core.Packages;
using Elsa.Catalog.Core.Sync;

namespace Elsa.Catalog.Api.Admin.Sync;

public sealed record AdminSyncRunResponse(
    Guid Id,
    SyncRunTrigger Trigger,
    SyncRunStatus Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    string? Error,
    string SummaryCountersJson,
    IReadOnlyList<AdminSyncRunItemResponse> Items);

public sealed record AdminSyncRunItemResponse(
    Guid Id,
    Guid? SourceId,
    string? PackageId,
    string? Version,
    SyncRunItemStatus Status,
    string? Message,
    string? Error,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt);

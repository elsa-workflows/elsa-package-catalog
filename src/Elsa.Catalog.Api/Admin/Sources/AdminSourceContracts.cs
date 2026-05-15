using Elsa.Catalog.Core.Packages;

namespace Elsa.Catalog.Api.Admin.Sources;

public sealed record AdminSourceRequest(
    string Name,
    string Url,
    bool Enabled,
    IReadOnlyList<string> IncludePatterns,
    IReadOnlyList<string>? ExcludePatterns,
    PackageSourceApprovalPolicy ApprovalPolicy,
    string? PollingInterval = null);

public sealed record AdminSourceResponse(
    Guid Id,
    string Name,
    PackageSourceType Type,
    string Url,
    bool Enabled,
    IReadOnlyList<string> IncludePatterns,
    IReadOnlyList<string> ExcludePatterns,
    PackageSourceApprovalPolicy ApprovalPolicy,
    PackageSourceStatus Status,
    DateTimeOffset? LastSyncedAt,
    DateTimeOffset? LastSuccessfulSyncAt,
    int PackageCount,
    DateTimeOffset? SoftDeletedAt,
    string? PollingInterval,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record AdminValidationErrorResponse(IReadOnlyList<string> Errors);

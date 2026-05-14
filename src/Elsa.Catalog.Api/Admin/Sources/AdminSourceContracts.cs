using Elsa.Catalog.Core.Packages;

namespace Elsa.Catalog.Api.Admin.Sources;

public sealed record AdminSourceRequest(
    string Name,
    string Url,
    bool Enabled,
    IReadOnlyList<string> IncludePatterns,
    IReadOnlyList<string>? ExcludePatterns,
    PackageSourceApprovalPolicy ApprovalPolicy);

public sealed record AdminSourceResponse(
    Guid Id,
    string Name,
    PackageSourceType Type,
    string Url,
    bool Enabled,
    IReadOnlyList<string> IncludePatterns,
    IReadOnlyList<string> ExcludePatterns,
    PackageSourceApprovalPolicy ApprovalPolicy,
    DateTimeOffset? LastSyncedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record AdminValidationErrorResponse(IReadOnlyList<string> Errors);

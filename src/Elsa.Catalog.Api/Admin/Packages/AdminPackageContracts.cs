using Elsa.Catalog.Core.Packages;

namespace Elsa.Catalog.Api.Admin.Packages;

public sealed record AdminPackageResponse(
    string PackageId,
    bool Approved,
    bool Listed,
    Guid SourceId,
    string? LatestVersion,
    PackageApprovalStatus ApprovalStatus,
    ValidationStatus ValidationStatus,
    int FeaturesCount,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<AdminPackageVersionResponse> Versions);

public sealed record AdminPackageVersionResponse(
    string Version,
    ValidationStatus ValidationStatus,
    PackageApprovalStatus ApprovalStatus,
    bool IsListed,
    bool SuspiciousChangeDetected,
    string? SchemaVersion);

public sealed record ApprovalRequest(string? Reason);

public sealed record AdminValidationResultResponse(
    Guid Id,
    string? SchemaVersion,
    ValidationStatus Status,
    string ErrorsJson,
    string WarningsJson,
    DateTimeOffset ValidatedAt,
    string? ValidatorVersion);

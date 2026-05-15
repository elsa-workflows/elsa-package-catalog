namespace Elsa.Catalog.Core.Packages;

public enum PackageSourceType
{
    NuGetFeed
}

public enum PackageSourceApprovalPolicy
{
    AutoApprove,
    Manual
}

public enum PackageApprovalStatus
{
    Pending,
    Approved,
    Rejected
}

public enum ValidationStatus
{
    NotValidated,
    Valid,
    Invalid,
    UnsupportedSchema,
    Suspicious
}

public enum SyncRunTrigger
{
    Scheduled,
    ManualAll,
    ManualSource,
    ManualPackage
}

public enum SyncRunStatus
{
    Running,
    Completed,
    Failed,
    CompletedWithErrors
}

public enum SyncRunItemStatus
{
    Discovered,
    Skipped,
    Downloaded,
    Indexed,
    Unchanged,
    Invalid,
    Failed,
    Suspicious
}

public enum ApprovalTargetType
{
    Package,
    PackageVersion
}

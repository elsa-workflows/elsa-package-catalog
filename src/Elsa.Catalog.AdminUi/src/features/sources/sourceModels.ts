export type SourceStatus = "Healthy" | "Warning" | "Error";
export type SourceApprovalPolicy = "AutoApprove" | "Manual";

export type PackageSource = {
  id: string;
  name: string;
  type: "NuGetFeed";
  url: string;
  enabled: boolean;
  includePatterns: string[];
  excludePatterns: string[];
  approvalPolicy: SourceApprovalPolicy;
  status: SourceStatus;
  isSyncing: boolean;
  lastSyncedAt?: string | null;
  lastSuccessfulSyncAt?: string | null;
  lastSyncError?: string | null;
  packageCount: number;
  softDeletedAt?: string | null;
  pollingInterval?: string | null;
  createdAt: string;
  updatedAt: string;
};

export type SourceFormValues = {
  name: string;
  url: string;
  enabled: boolean;
  approvalPolicy: SourceApprovalPolicy;
  includePatterns: string;
  excludePatterns: string;
  pollingInterval: string;
};

export function toSourceFormValues(source?: PackageSource): SourceFormValues {
  return {
    name: source?.name ?? "",
    url: source?.url ?? "",
    enabled: source?.enabled ?? true,
    approvalPolicy: source?.approvalPolicy ?? "Manual",
    includePatterns: source?.includePatterns.join("\n") ?? "Elsa.*",
    excludePatterns: source?.excludePatterns.join("\n") ?? "",
    pollingInterval: source?.pollingInterval ?? "PT30M"
  };
}

export function toSourceRequest(values: SourceFormValues) {
  return {
    name: values.name.trim(),
    url: values.url.trim(),
    enabled: values.enabled,
    approvalPolicy: values.approvalPolicy,
    includePatterns: splitPatterns(values.includePatterns),
    excludePatterns: splitPatterns(values.excludePatterns),
    pollingInterval: values.pollingInterval.trim() || null
  };
}

export function splitPatterns(value: string) {
  return value
    .split(/\r?\n|,/)
    .map((item) => item.trim())
    .filter(Boolean);
}

export function sourceHealthText(source: PackageSource, isSyncing = false) {
  if (isSyncing || source.isSyncing) return "Syncing";
  if (source.status === "Error") return "Sync failing";
  if (source.status === "Warning") return "Needs review";
  return source.enabled ? "Healthy" : "Disabled";
}

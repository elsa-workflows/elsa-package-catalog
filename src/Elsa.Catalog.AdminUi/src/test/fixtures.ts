export const sourceFixture = {
  id: "source-1",
  name: "Elsa Official",
  type: "NuGetFeed",
  url: "https://api.nuget.org/v3/index.json",
  enabled: true,
  includePatterns: ["Elsa.*"],
  excludePatterns: ["*.Tests"],
  approvalPolicy: "Manual",
  versionDiscoveryPolicy: "AllVersions",
  status: "Healthy",
  isSyncing: false,
  lastSuccessfulSyncAt: "2026-05-15T08:00:00Z",
  lastSyncedAt: "2026-05-15T08:00:00Z",
  lastSyncError: null,
  packageCount: 12,
  createdAt: "2026-05-15T07:00:00Z",
  updatedAt: "2026-05-15T08:00:00Z"
};

export const packageFixture = {
  packageId: "Elsa.Persistence.PostgreSql",
  sourceId: "00000000-0000-0000-0000-000000000001",
  approved: false,
  listed: true,
  latestVersion: "1.0.2",
  approvalStatus: "Pending",
  validationStatus: "Valid",
  featuresCount: 3,
  updatedAt: "2026-05-15T08:15:00Z",
  versions: [
    {
      version: "1.0.2",
      approvalStatus: "Pending",
      validationStatus: "Valid",
      isListed: true,
      suspiciousChangeDetected: false,
      schemaVersion: "1.0"
    }
  ]
};

export const syncRunFixture = {
  id: "sync-123",
  trigger: "Scheduled",
  status: "CompletedWithErrors",
  startedAt: "2026-05-15T08:00:00Z",
  completedAt: "2026-05-15T08:02:14Z",
  error: null,
  summaryCountersJson: JSON.stringify({ scanned: 52, indexed: 4, failed: 1 }),
  itemCount: 1,
  sources: [{ id: "source-1", name: "Elsa Official" }],
  items: [
    {
      id: "item-1",
      sourceId: "source-1",
      packageId: "Elsa.Persistence.PostgreSql",
      version: "1.0.2",
      status: "Failed",
      message: null,
      error: "Package download failed.",
      startedAt: "2026-05-15T08:00:05Z",
      completedAt: "2026-05-15T08:00:07Z"
    }
  ]
};

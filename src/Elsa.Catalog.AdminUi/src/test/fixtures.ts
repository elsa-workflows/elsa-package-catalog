export const sourceFixture = {
  id: "source-1",
  name: "Elsa Official",
  type: "NuGetFeed",
  url: "https://api.nuget.org/v3/index.json",
  enabled: true,
  includePatterns: ["Elsa.*"],
  excludePatterns: ["*.Tests"],
  approvalPolicy: "Manual",
  status: "Healthy",
  lastSuccessfulSyncAt: "2026-05-15T08:00:00Z",
  lastSyncedAt: "2026-05-15T08:00:00Z",
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
  packagesScanned: 52,
  packagesUpdated: 4,
  failures: 1,
  items: []
};

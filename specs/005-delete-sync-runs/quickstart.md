# Quickstart: Delete Sync Runs

## Local Verification

1. Start the API host with admin authentication configured.
2. Seed or create several sync runs:
   - At least one completed run older than the intended cutoff.
   - At least one failed or completed-with-errors run older than the cutoff.
   - At least one canceled run older than the cutoff.
   - At least one recent completed run.
   - At least one running run.
3. Call `GET /api/admin/sync-runs/deletion-preview?completedBefore=<UTC cutoff>` with admin credentials.
4. Confirm the preview reports old terminal runs, including canceled runs, as eligible and excludes the running run.
5. Call `DELETE /api/admin/sync-runs/{id}` for one old terminal run.
6. Confirm the response reports one deleted run and the expected item count.
7. Call `GET /api/admin/sync-runs/{id}` and confirm the deleted run is no longer found.
8. Confirm package sources, packages, package versions, manifests, validation results, and approvals are still present.
9. Call `DELETE /api/admin/sync-runs?completedBefore=<UTC cutoff>` for bulk cleanup.
10. Confirm only eligible old terminal runs are removed and recent or running runs remain visible.

## Retention Worker Verification

1. Enable retention with `Sync:Retention:Enabled=true`.
2. Set `Sync:Retention:RetentionDays=30`, `Sync:Retention:Interval=24:00:00`, and `Sync:Retention:RunOnStartup=true`.
3. Start the API with old terminal sync runs already present.
4. Confirm startup logs include retention cleanup counts and terminal runs older than 30 days are deleted.
5. Confirm recent and running sync runs remain visible.

## Admin UI Verification

1. Open `/admin/sync-runs`.
2. Confirm terminal runs show a delete action and running runs do not.
3. Choose a bulk cleanup cutoff and preview deletion.
4. Confirm the dialog shows eligible run and item counts.
5. Confirm cleanup and verify the Sync Runs list refreshes.

## Automated Verification

Run:

```sh
dotnet test tests/Elsa.Catalog.Core.Tests/Elsa.Catalog.Core.Tests.csproj --filter SyncRunCleanup
dotnet test tests/Elsa.Catalog.Persistence.EntityFrameworkCore.Tests/Elsa.Catalog.Persistence.EntityFrameworkCore.Tests.csproj --filter SyncPersistence
dotnet test tests/Elsa.Catalog.Api.Tests/Elsa.Catalog.Api.Tests.csproj --filter "AdminSync|SyncRunRetention"
cd src/Elsa.Catalog.AdminUi && npm test -- --run src/features/sync-runs/SyncRunsPage.test.tsx src/features/sync-runs/syncRunModels.test.ts
```

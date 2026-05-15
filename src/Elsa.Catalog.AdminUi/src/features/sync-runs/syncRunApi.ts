import { apiRequest } from "@/lib/api/httpClient";
import type { SyncRun } from "@/features/sync-runs/syncRunModels";
import { normalizeSyncRun, normalizeSyncRuns } from "@/features/sync-runs/syncRunModels";

export async function listSyncRuns() {
  return normalizeSyncRuns(await apiRequest<unknown>("/api/admin/sync-runs"));
}

export async function getSyncRun(runId: string) {
  return normalizeSyncRun(await apiRequest<unknown>(`/api/admin/sync-runs/${runId}`));
}

export async function syncAll() {
  return normalizeSyncRun(await apiRequest<SyncRun>("/api/admin/sync", { method: "POST" }));
}

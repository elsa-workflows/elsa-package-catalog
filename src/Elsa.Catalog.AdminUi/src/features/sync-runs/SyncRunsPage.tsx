import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Play, RefreshCw, Search, X } from "lucide-react";
import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { Badge, Button, EmptyState, Input, SecondaryButton, Select, Table } from "@/components/ui";
import { RequestStateView } from "@/components/states/RequestStateViews";
import { listSyncRuns, syncAll } from "@/features/sync-runs/syncRunApi";
import type { SyncRunStatus, SyncRunTrigger } from "@/features/sync-runs/syncRunModels";
import {
  isActiveSyncRun,
  packagesScanned,
  packagesUpdated,
  shortId,
  syncFailures,
  syncRunHasAttention,
  syncRunStatusLabel,
  syncRunTriggerLabel
} from "@/features/sync-runs/syncRunModels";
import { formatDateTime, formatDuration } from "@/lib/formatters";
import { queryKeys } from "@/lib/query/queryClient";
import { sourceStatusTone, statusToneClass } from "@/lib/status/statusBadges";

const statuses: Array<SyncRunStatus | "All"> = ["All", "Running", "Completed", "CompletedWithErrors", "Failed"];
const triggers: Array<SyncRunTrigger | "All"> = ["All", "Scheduled", "ManualAll", "ManualSource", "ManualPackage"];

export function SyncRunsPage() {
  const [filter, setFilter] = useState("");
  const [status, setStatus] = useState<SyncRunStatus | "All">("All");
  const [trigger, setTrigger] = useState<SyncRunTrigger | "All">("All");
  const queryClient = useQueryClient();
  const syncRuns = useQuery({ queryKey: queryKeys.syncRuns, queryFn: listSyncRuns, refetchInterval: 15_000 });
  const startSync = useMutation({
    mutationFn: syncAll,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.syncRuns })
  });

  const filtered = useMemo(() => {
    const term = filter.trim().toLowerCase();
    return (syncRuns.data ?? []).filter((run) => {
      const matchesTerm = !term || `${run.id} ${run.status} ${run.trigger} ${run.error ?? ""}`.toLowerCase().includes(term);
      const matchesStatus = status === "All" || run.status === status;
      const matchesTrigger = trigger === "All" || run.trigger === trigger;
      return matchesTerm && matchesStatus && matchesTrigger;
    });
  }, [filter, status, syncRuns.data, trigger]);

  const hasFilters = Boolean(filter.trim()) || status !== "All" || trigger !== "All";
  const hasActiveRun = (syncRuns.data ?? []).some(isActiveSyncRun);

  function clearFilters() {
    setFilter("");
    setStatus("All");
    setTrigger("All");
  }

  if (syncRuns.isLoading) return <RequestStateView state="loading" title="Loading sync runs" />;
  if (syncRuns.isError && !syncRuns.data) return <RequestStateView state="unexpected" title="Sync runs could not load" />;

  return (
    <section className="space-y-5">
      <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
        <div>
          <h1 className="text-xl font-semibold">Sync Runs</h1>
          <p className="mt-1 text-sm text-muted-foreground">Review synchronization history, outcomes, and item diagnostics.</p>
        </div>
        <div className="flex flex-wrap gap-2">
          <SecondaryButton onClick={() => syncRuns.refetch()} title="Refresh sync runs">
            <RefreshCw className="h-4 w-4" />
            Refresh
          </SecondaryButton>
          <Button onClick={() => startSync.mutate()} disabled={startSync.isPending || hasActiveRun} title="Sync all enabled sources">
            <Play className="h-4 w-4" />
            Sync All
          </Button>
        </div>
      </div>

      {syncRuns.isRefetchError ? <RequestStateView state="stale" title="Showing last loaded sync runs" /> : null}
      {startSync.isError ? <RequestStateView state="unexpected" title="Sync could not start" /> : null}

      <div className="flex flex-col gap-2 lg:flex-row lg:items-center">
        <label className="relative block w-full max-w-md">
          <Search className="pointer-events-none absolute left-3 top-2.5 h-4 w-4 text-muted-foreground" />
          <Input value={filter} onChange={(event) => setFilter(event.target.value)} className="pl-9" placeholder="Filter sync runs" />
        </label>
        <Select aria-label="Filter by status" value={status} onChange={(event) => setStatus(event.target.value as SyncRunStatus | "All")}>
          {statuses.map((option) => (
            <option key={option} value={option}>
              {option === "All" ? "All statuses" : syncRunStatusLabel(option)}
            </option>
          ))}
        </Select>
        <Select aria-label="Filter by trigger" value={trigger} onChange={(event) => setTrigger(event.target.value as SyncRunTrigger | "All")}>
          {triggers.map((option) => (
            <option key={option} value={option}>
              {option === "All" ? "All triggers" : syncRunTriggerLabel(option)}
            </option>
          ))}
        </Select>
        {hasFilters ? (
          <SecondaryButton onClick={clearFilters} title="Clear filters">
            <X className="h-4 w-4" />
            Clear
          </SecondaryButton>
        ) : null}
      </div>

      {(syncRuns.data ?? []).length === 0 ? (
        <EmptyState title="No sync runs" description="Run a source sync from Sources or start a full sync here." />
      ) : filtered.length === 0 ? (
        <EmptyState title="No matching sync runs" description="Clear the filters to see all synchronization history." />
      ) : (
        <Table>
          <table className="min-w-full divide-y divide-border text-sm">
            <thead className="bg-muted/40 text-left text-xs uppercase text-muted-foreground">
              <tr>
                <th className="px-3 py-2">Started</th>
                <th className="px-3 py-2">Duration</th>
                <th className="px-3 py-2">Trigger</th>
                <th className="px-3 py-2">Status</th>
                <th className="px-3 py-2">Scanned</th>
                <th className="px-3 py-2">Updated</th>
                <th className="px-3 py-2">Failures</th>
                <th className="px-3 py-2">Items</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {filtered.map((run) => (
                <tr key={run.id} className={syncRunHasAttention(run) ? "bg-destructive/5" : undefined}>
                  <td className="px-3 py-3 font-medium">
                    <Link to={`/admin/sync-runs/${run.id}`}>{formatDateTime(run.startedAt)}</Link>
                    <div className="mt-1 font-mono text-xs text-muted-foreground">{shortId(run.id)}</div>
                  </td>
                  <td className="px-3 py-3">{formatDuration(run.startedAt, run.completedAt)}</td>
                  <td className="px-3 py-3">{syncRunTriggerLabel(run.trigger)}</td>
                  <td className="px-3 py-3">
                    <Badge className={statusToneClass(sourceStatusTone(run.status))}>{syncRunStatusLabel(run.status)}</Badge>
                  </td>
                  <td className="px-3 py-3">{packagesScanned(run)}</td>
                  <td className="px-3 py-3">{packagesUpdated(run)}</td>
                  <td className="px-3 py-3">{syncFailures(run)}</td>
                  <td className="px-3 py-3">{run.items.length}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </Table>
      )}
    </section>
  );
}

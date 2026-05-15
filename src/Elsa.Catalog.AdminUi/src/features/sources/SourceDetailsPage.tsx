import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import type { ReactNode } from "react";
import { Link, useParams } from "react-router-dom";
import { Badge, Button, SecondaryButton } from "@/components/ui";
import { RequestStateView } from "@/components/states/RequestStateViews";
import { SourceActions } from "@/features/sources/SourceActions";
import { getSource, syncSource } from "@/features/sources/sourceApi";
import { sourceHealthText } from "@/features/sources/sourceModels";
import { formatDateTime } from "@/lib/formatters";
import { queryKeys } from "@/lib/query/queryClient";
import { sourceStatusTone, statusToneClass } from "@/lib/status/statusBadges";

export function SourceDetailsPage() {
  const { sourceId } = useParams();
  const queryClient = useQueryClient();
  const source = useQuery({ queryKey: [...queryKeys.sources, sourceId], queryFn: () => getSource(sourceId!), enabled: Boolean(sourceId) });
  const sync = useMutation({
    mutationFn: () => syncSource(sourceId!),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.sources })
  });

  if (source.isLoading) return <RequestStateView state="loading" title="Loading source" />;
  if (source.isError || !source.data) return <RequestStateView state="not-found" title="Source not found" />;

  return (
    <section className="space-y-5">
      <div className="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
        <div>
          <h1 className="text-xl font-semibold">{source.data.name}</h1>
          <p className="mt-1 break-all text-sm text-muted-foreground">{source.data.url}</p>
        </div>
        <div className="flex gap-2">
          <SecondaryButton><Link to={`/admin/sources/${source.data.id}/edit`}>Edit</Link></SecondaryButton>
          <Button onClick={() => sync.mutate()} disabled={sync.isPending}>Sync Now</Button>
        </div>
      </div>
      <div className="grid gap-3 md:grid-cols-3">
        <Info label="Health" value={<Badge className={statusToneClass(sourceStatusTone(source.data.status))}>{sourceHealthText(source.data)}</Badge>} />
        <Info label="Last successful sync" value={formatDateTime(source.data.lastSuccessfulSyncAt)} />
        <Info label="Package count" value={source.data.packageCount} />
        <Info label="Polling interval" value={source.data.pollingInterval ?? "Manual"} />
        <Info label="Approval policy" value={source.data.approvalPolicy} />
        <Info label="Enabled" value={source.data.enabled ? "Yes" : "No"} />
      </div>
      <div className="rounded-ui border border-border p-4">
        <h2 className="text-sm font-medium">Indexing boundaries</h2>
        <div className="mt-3 grid gap-4 md:grid-cols-2">
          <PatternList title="Include" items={source.data.includePatterns} />
          <PatternList title="Exclude" items={source.data.excludePatterns} />
        </div>
      </div>
      <SourceActions source={source.data} />
    </section>
  );
}

function Info({ label, value }: { label: string; value: ReactNode }) {
  return (
    <div className="rounded-ui border border-border p-4">
      <div className="text-xs uppercase text-muted-foreground">{label}</div>
      <div className="mt-2 text-sm font-medium">{value}</div>
    </div>
  );
}

function PatternList({ title, items }: { title: string; items: string[] }) {
  return (
    <div>
      <div className="text-xs uppercase text-muted-foreground">{title}</div>
      <ul className="mt-2 space-y-1 font-mono text-sm">
        {items.length ? items.map((item) => <li key={item}>{item}</li>) : <li className="text-muted-foreground">None</li>}
      </ul>
    </div>
  );
}

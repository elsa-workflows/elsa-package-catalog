import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Play, Power, Trash2 } from "lucide-react";
import { useState } from "react";
import { Button, DialogPanel, SecondaryButton } from "@/components/ui";
import { deleteSource, setSourceEnabled, syncSource } from "@/features/sources/sourceApi";
import type { PackageSource } from "@/features/sources/sourceModels";
import { queryKeys } from "@/lib/query/queryClient";

export function SourceActions({ source }: { source: PackageSource }) {
  const [confirmingDelete, setConfirmingDelete] = useState(false);
  const queryClient = useQueryClient();
  const invalidate = () => queryClient.invalidateQueries({ queryKey: queryKeys.sources });
  const sync = useMutation({ mutationFn: () => syncSource(source.id), onSuccess: invalidate });
  const toggle = useMutation({ mutationFn: () => setSourceEnabled(source, !source.enabled), onSuccess: invalidate });
  const remove = useMutation({ mutationFn: () => deleteSource(source.id), onSuccess: invalidate });

  return (
    <div className="flex flex-wrap items-center gap-2">
      <SecondaryButton onClick={() => sync.mutate()} disabled={sync.isPending} title="Sync now">
        <Play className="h-4 w-4" />
        Sync
      </SecondaryButton>
      <SecondaryButton onClick={() => toggle.mutate()} disabled={toggle.isPending} title={source.enabled ? "Disable source" : "Enable source"}>
        <Power className="h-4 w-4" />
        {source.enabled ? "Disable" : "Enable"}
      </SecondaryButton>
      <SecondaryButton onClick={() => setConfirmingDelete(true)} className="text-destructive" title="Soft-delete source">
        <Trash2 className="h-4 w-4" />
        Delete
      </SecondaryButton>
      {confirmingDelete ? (
        <div className="fixed inset-0 z-20 flex items-center justify-center bg-background/70 p-4">
          <DialogPanel>
            <div className="max-w-sm space-y-4">
              <div>
                <h2 className="font-medium">Delete {source.name}?</h2>
                <p className="mt-1 text-sm text-muted-foreground">The source is hidden from admin reads and syncs, but package history is preserved.</p>
              </div>
              <div className="flex justify-end gap-2">
                <SecondaryButton onClick={() => setConfirmingDelete(false)}>Cancel</SecondaryButton>
                <Button onClick={() => remove.mutate()} disabled={remove.isPending} className="bg-destructive text-white">
                  Delete
                </Button>
              </div>
            </div>
          </DialogPanel>
        </div>
      ) : null}
    </div>
  );
}

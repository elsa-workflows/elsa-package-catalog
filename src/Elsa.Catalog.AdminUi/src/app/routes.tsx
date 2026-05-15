import { createBrowserRouter, Navigate } from "react-router-dom";
import { AppShell } from "@/app/AppShell";
import { RequestStateView } from "@/components/states/RequestStateViews";
import { NewSourcePage, EditSourcePage } from "@/features/sources/SourceFormPage";
import { SourceDetailsPage } from "@/features/sources/SourceDetailsPage";
import { SourcesPage } from "@/features/sources/SourcesPage";

function PlaceholderPage({ title }: { title: string }) {
  return (
    <section className="space-y-2">
      <h1 className="text-xl font-semibold">{title}</h1>
      <p className="text-sm text-muted-foreground">This operational view is ready for feature implementation.</p>
    </section>
  );
}

export const router = createBrowserRouter([
  {
    path: "/",
    element: <Navigate to="/admin/overview" replace />
  },
  {
    path: "/admin",
    element: <AppShell />,
    errorElement: <RequestStateView state="unexpected" title="The admin dashboard could not load." />,
    children: [
      { index: true, element: <Navigate to="/admin/overview" replace /> },
      { path: "overview", element: <PlaceholderPage title="Overview" /> },
      { path: "sources", element: <SourcesPage /> },
      { path: "sources/new", element: <NewSourcePage /> },
      { path: "sources/:sourceId", element: <SourceDetailsPage /> },
      { path: "sources/:sourceId/edit", element: <EditSourcePage /> },
      { path: "packages", element: <PlaceholderPage title="Packages" /> },
      { path: "packages/:packageId", element: <PlaceholderPage title="Package Details" /> },
      { path: "packages/:packageId/versions/:version", element: <PlaceholderPage title="Package Version" /> },
      { path: "sync-runs", element: <PlaceholderPage title="Sync Runs" /> },
      { path: "sync-runs/:runId", element: <PlaceholderPage title="Sync Run Details" /> }
    ]
  }
]);

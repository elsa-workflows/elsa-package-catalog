import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ReactNode } from "react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { SyncRunDetailsPage } from "@/features/sync-runs/SyncRunDetailsPage";
import { SyncRunsPage } from "@/features/sync-runs/SyncRunsPage";
import { syncRunFixture } from "@/test/fixtures";

function renderWithQueryClient(ui: ReactNode, response: unknown, status = 200, routePath?: string) {
  vi.stubGlobal("fetch", vi.fn(async () => new Response(JSON.stringify(response), { status, headers: { "Content-Type": "application/json" } })));
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/admin/sync-runs/${syncRunFixture.id}`]}>
        {routePath ? (
          <Routes>
            <Route path={routePath} element={ui} />
          </Routes>
        ) : (
          ui
        )}
      </MemoryRouter>
    </QueryClientProvider>
  );
}

afterEach(() => {
  vi.unstubAllGlobals();
});

describe("SyncRunsPage", () => {
  it("shows loading then empty state", async () => {
    renderWithQueryClient(<SyncRunsPage />, []);

    expect(screen.getByText("Loading sync runs")).toBeInTheDocument();
    expect(await screen.findByText("No sync runs")).toBeInTheDocument();
  });

  it("shows populated sync run rows with counters", async () => {
    renderWithQueryClient(<SyncRunsPage />, [syncRunFixture]);

    expect((await screen.findAllByText("Completed with errors")).length).toBeGreaterThan(0);
    expect(screen.getByRole("link", { name: "Elsa Official" })).toHaveAttribute("href", "/admin/sources/source-1");
    expect(screen.getByText("52")).toBeInTheDocument();
    expect(screen.getByText("4")).toBeInTheDocument();
    expect(screen.getAllByText("1").length).toBeGreaterThan(0);
  });

  it("filters populated sync run rows", async () => {
    renderWithQueryClient(<SyncRunsPage />, [syncRunFixture]);

    await screen.findAllByText("Completed with errors");
    await userEvent.type(screen.getByPlaceholderText("Filter sync runs"), "missing-source");

    expect(screen.getByText("No matching sync runs")).toBeInTheDocument();
  });

  it("filters populated sync run rows by status", async () => {
    renderWithQueryClient(<SyncRunsPage />, [syncRunFixture]);

    await screen.findAllByText("Completed with errors");
    await userEvent.selectOptions(screen.getByLabelText("Filter by status"), "Running");

    expect(screen.getByText("No matching sync runs")).toBeInTheDocument();
  });

  it("shows error state when no sync run data is available", async () => {
    renderWithQueryClient(<SyncRunsPage />, { title: "Unavailable" }, 503);

    expect(await screen.findByText("Sync runs could not load")).toBeInTheDocument();
  });
});

describe("SyncRunDetailsPage", () => {
  it("shows run diagnostics and failed items", async () => {
    renderWithQueryClient(<SyncRunDetailsPage />, syncRunFixture, 200, "/admin/sync-runs/:runId");

    expect(await screen.findByText("Sync Run sync-123")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Elsa Official" })).toHaveAttribute("href", "/admin/sources/source-1");
    expect(screen.getAllByText("Package download failed.").length).toBeGreaterThan(0);
    expect(screen.getByRole("link", { name: "Elsa.Persistence.PostgreSql" })).toBeInTheDocument();
  });

  it("shows when diagnostic panels are abbreviated", async () => {
    const failedItems = Array.from({ length: 6 }, (_, index) => ({
      ...syncRunFixture.items[0],
      id: `item-${index + 1}`,
      packageId: `Elsa.Failed.${index + 1}`
    }));

    renderWithQueryClient(<SyncRunDetailsPage />, { ...syncRunFixture, items: failedItems }, 200, "/admin/sync-runs/:runId");

    expect(await screen.findByText("1 more item is shown in the full table below.")).toBeInTheDocument();
  });
});

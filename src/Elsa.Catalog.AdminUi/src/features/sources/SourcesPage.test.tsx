import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { SourcesPage } from "@/features/sources/SourcesPage";
import { sourceFixture } from "@/test/fixtures";

function renderSourcesPage(response: unknown, status = 200) {
  vi.stubGlobal("fetch", vi.fn(async () => new Response(JSON.stringify(response), { status, headers: { "Content-Type": "application/json" } })));
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <SourcesPage />
      </MemoryRouter>
    </QueryClientProvider>
  );
}

afterEach(() => {
  vi.unstubAllGlobals();
});

describe("SourcesPage", () => {
  it("shows loading then empty state", async () => {
    renderSourcesPage([]);

    expect(screen.getByText("Loading sources")).toBeInTheDocument();
    expect(await screen.findByText("No package sources")).toBeInTheDocument();
  });

  it("shows populated source rows with health and sync evidence", async () => {
    renderSourcesPage([sourceFixture]);

    expect(await screen.findByRole("link", { name: sourceFixture.name })).toBeInTheDocument();
    expect(screen.getByText("Healthy")).toBeInTheDocument();
    expect(screen.getByText("12")).toBeInTheDocument();
  });

  it("links each source row to the edit form", async () => {
    renderSourcesPage([sourceFixture]);

    expect(await screen.findByRole("link", { name: "Edit" })).toHaveAttribute("href", "/admin/sources/source-1/edit");
  });

  it("filters populated source rows", async () => {
    renderSourcesPage([sourceFixture]);

    await screen.findByRole("link", { name: sourceFixture.name });
    await userEvent.type(screen.getByPlaceholderText("Filter sources"), "missing");

    expect(screen.getByText("No matching sources")).toBeInTheDocument();
  });

  it("shows error state when no source data is available", async () => {
    renderSourcesPage({ title: "Unavailable" }, 503);

    expect(await screen.findByText("Sources could not load")).toBeInTheDocument();
  });
});

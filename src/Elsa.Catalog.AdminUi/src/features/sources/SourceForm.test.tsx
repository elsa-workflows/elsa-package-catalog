import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import { SourceForm } from "@/features/sources/SourceForm";

function renderForm(onSubmit = vi.fn(async () => undefined)) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <SourceForm onSubmit={onSubmit} />
      </MemoryRouter>
    </QueryClientProvider>
  );
  return onSubmit;
}

describe("SourceForm", () => {
  it("validates required fields before saving", async () => {
    const onSubmit = renderForm();
    await userEvent.clear(screen.getByLabelText("Name"));
    await userEvent.clear(screen.getByLabelText("Feed URL"));

    await userEvent.click(screen.getByRole("button", { name: "Save Source" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Name and feed URL are required.");
    expect(onSubmit).not.toHaveBeenCalled();
  });

  it("preserves unsaved values after a failed save", async () => {
    renderForm(vi.fn(async () => {
      throw new Error("Validation failed");
    }));
    await userEvent.type(screen.getByLabelText("Name"), "Internal NuGet");
    await userEvent.type(screen.getByLabelText("Feed URL"), "https://example.test/v3/index.json");

    await userEvent.click(screen.getByRole("button", { name: "Save Source" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Validation failed");
    expect(screen.getByLabelText("Name")).toHaveValue("Internal NuGet");
  });

  it("shows pattern tester preview while editing patterns", async () => {
    renderForm();
    expect(screen.getByText("Elsa.Persistence.PostgreSql")).toBeInTheDocument();
    expect(screen.getByText("Elsa.Tests").previousSibling).toHaveTextContent("OK");

    await userEvent.type(screen.getByLabelText("Exclude Patterns"), "*.Tests");

    expect(screen.getByText("Elsa.Tests").previousSibling).toHaveTextContent("NO");
  });
});

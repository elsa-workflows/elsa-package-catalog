import { render, screen } from "@testing-library/react";
import { createMemoryRouter, RouterProvider } from "react-router-dom";
import { describe, expect, it } from "vitest";
import { AppShell } from "@/app/AppShell";

describe("AppShell", () => {
  it("renders the four MVP navigation entries", () => {
    const router = createMemoryRouter([{ path: "/admin", element: <AppShell /> }], {
      initialEntries: ["/admin"]
    });

    render(<RouterProvider router={router} />);

    expect(screen.getAllByRole("link", { name: "Overview" }).length).toBeGreaterThan(0);
    expect(screen.getAllByRole("link", { name: "Sources" }).length).toBeGreaterThan(0);
    expect(screen.getAllByRole("link", { name: "Packages" }).length).toBeGreaterThan(0);
    expect(screen.getAllByRole("link", { name: "Sync Runs" }).length).toBeGreaterThan(0);
    expect(screen.queryByRole("link", { name: "Settings" })).not.toBeInTheDocument();
  });
});

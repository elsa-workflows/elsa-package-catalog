import { Boxes, DatabaseZap, Home, PackageSearch } from "lucide-react";
import { NavLink, Outlet } from "react-router-dom";
import { cn } from "@/lib/utils";

const navItems = [
  { to: "/admin/overview", label: "Overview", icon: Home },
  { to: "/admin/sources", label: "Sources", icon: DatabaseZap },
  { to: "/admin/packages", label: "Packages", icon: PackageSearch },
  { to: "/admin/sync-runs", label: "Sync Runs", icon: Boxes }
];

export function AppShell() {
  return (
    <div className="min-h-screen bg-background text-foreground">
      <aside className="fixed inset-y-0 left-0 hidden w-64 border-r border-border bg-surface px-3 py-4 md:block">
        <div className="px-2 pb-6">
          <p className="text-sm font-semibold">Elsa Package Catalog</p>
          <p className="text-xs text-muted-foreground">Admin</p>
        </div>
        <nav aria-label="Primary" className="space-y-1">
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) =>
                cn(
                  "flex items-center gap-2 rounded-ui px-3 py-2 text-sm transition-colors",
                  isActive ? "bg-muted text-foreground" : "text-muted-foreground hover:bg-muted hover:text-foreground"
                )
              }
            >
              <item.icon aria-hidden className="h-4 w-4" />
              {item.label}
            </NavLink>
          ))}
        </nav>
      </aside>
      <div className="md:pl-64">
        <header className="sticky top-0 z-10 border-b border-border bg-background/95 px-4 py-3 backdrop-blur md:hidden">
          <nav aria-label="Primary" className="flex gap-1 overflow-x-auto">
            {navItems.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                className={({ isActive }) =>
                  cn(
                    "whitespace-nowrap rounded-ui px-3 py-2 text-sm",
                    isActive ? "bg-muted text-foreground" : "text-muted-foreground"
                  )
                }
              >
                {item.label}
              </NavLink>
            ))}
          </nav>
        </header>
        <main className="mx-auto max-w-7xl px-4 py-6 md:px-8">
          <Outlet />
        </main>
      </div>
    </div>
  );
}

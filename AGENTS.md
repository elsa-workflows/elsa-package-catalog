<!-- SPECKIT START -->
For additional context about technologies to be used, project structure,
shell commands, and other important information, read the current plan:
specs/004-admin-dashboard-auth/plan.md
<!-- SPECKIT END -->

## Spec Kit Workflow

- Major features, user-facing behavior changes, and cross-cutting architecture changes should go through Spec Kit before implementation.
- Follow-up adjustments to an existing feature should update that feature's existing Spec Kit artifacts (`spec.md`, `plan.md`, contracts, tasks, and quickstart as applicable) when the change belongs to the same feature scope.
- Create a new Spec Kit feature only when the change introduces a distinct capability, policy, or workflow that does not fit an existing spec.
- Keep implementation, tests, and documentation aligned with the active or amended Spec Kit artifacts before considering the work complete.

## Active Technologies
- C# on .NET 10 LTS + ASP.NET Core, Entity Framework Core, SQLite, NuGet.Protocol, System.Text.Json, JSON Schema validation, xUnit, FluentAssertions (001-package-catalog)
- SQLite for initial durable storage with provider-neutral EF Core/domain design for later PostgreSQL support (001-package-catalog)
- C# on .NET 10 LTS + MSBuild task APIs, System.Reflection.Metadata, MetadataLoadContext, System.Xml.Linq, System.Text.Json, JsonSchema.Net, Elsa.PackageManifests, NuGet.Versioning, xUnit, FluentAssertions (002-package-manifest-generator)
- File artifact generation only: compiled assemblies, XML docs, project/NuGet metadata, reference metadata, optional overrides in; deterministic `elsa-package.json` and NuGet package root inclusion out (002-package-manifest-generator)
- TypeScript admin web UI + React, React Router, TanStack Query, TailwindCSS, shadcn/ui-style components, existing authenticated Catalog Admin REST APIs (003-admin-dashboard-ui)
- No frontend-owned durable storage; transient UI state only, with durable catalog state remaining in existing backend persistence (003-admin-dashboard-ui)
- C# on .NET 10 LTS with nullable reference types + existing MSBuild task APIs, metadata inspection, System.Text.Json, Elsa.PackageManifests validation, xUnit, FluentAssertions (003-generator-adoption-fixes)
- File artifact generation only: compiled assemblies/XML docs/project metadata in; deterministic `elsa-package.json` and NuGet package root inclusion out (003-generator-adoption-fixes)
- C# on .NET 10 LTS for API host; existing React + TypeScript admin UI remains a static asset build. + ASP.NET Core authentication/authorization, cookie authentication, existing custom API key authentication handler, existing admin UI build output. (004-admin-dashboard-auth)
- Existing configuration secret for the admin API key; HTTP-only auth cookie for dashboard sessions; in-memory per-client failed-login throttle only. No new durable storage. (004-admin-dashboard-auth)

## Recent Changes
- 001-package-catalog: Added implementation plan, research, data model, OpenAPI contract, quickstart, and current plan reference.
- 001-package-catalog: Shifted project structure to onion-style `Elsa.Catalog.Core`, `Elsa.Catalog.Persistence.EntityFrameworkCore`, and `Elsa.Catalog.Packaging.NuGet`.
- 002-package-manifest-generator: Added implementation plan, research, data model, MSBuild/annotation/package-layout contracts, override schema, and quickstart.
- 003-admin-dashboard-ui: Added implementation plan, research, data model, admin API/UI route contracts, and quickstart for the lightweight operational admin dashboard.
- 003-generator-adoption-fixes: Added plan, research, data model, diagnostic policy, setting discovery, package inclusion contracts, and quickstart.
- 003-generator-adoption-fixes: Amended setting discovery policy so unsupported non-manifestable setting properties are omitted with low-importance diagnostics instead of failing normal builds.

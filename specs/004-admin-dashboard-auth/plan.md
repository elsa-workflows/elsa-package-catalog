# Implementation Plan: Admin Dashboard Authentication

**Branch**: `004-admin-dashboard-auth` | **Date**: 2026-05-15 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/004-admin-dashboard-auth/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

Protect the deployed admin dashboard from anonymous access by adding a small app-owned cookie session flow backed by the existing configured admin API key. The React dashboard remains unchanged as the operational UI, while the ASP.NET Core host serves a minimal login/logout surface, gates `/admin` dashboard assets, and allows admin REST APIs to authenticate with either the existing API key header or the new dashboard session cookie.

## Technical Context

<!--
  ACTION REQUIRED: Replace the content in this section with the technical details
  for the project. The structure here is presented in advisory capacity to guide
  the iteration process.
-->

**Language/Version**: C# on .NET 10 LTS for API host; existing React + TypeScript admin UI remains a static asset build.

**Primary Dependencies**: ASP.NET Core authentication/authorization, existing custom API key authentication handler, existing admin UI build output.

**Storage**: Existing configuration secret for the admin API key; no new persistent storage.

**Testing**: xUnit, FluentAssertions, ASP.NET Core WebApplicationFactory integration tests.

**Target Platform**: ASP.NET Core API container deployed to Azure App Service.

**Project Type**: Web service hosting REST APIs plus static admin UI assets.

**Performance Goals**: Authentication checks add negligible overhead to dashboard and admin API requests.

**Constraints**: Keep auth small; no OIDC, RBAC, user database, or frontend key storage in this feature.

**Scale/Scope**: Internal admin dashboard for a small number of operators.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The plan MUST answer these gates:

- **Manifest-first**: Does package metadata flow through explicit, versioned manifests rather than package code execution?
  - Not impacted. This feature only changes dashboard access.
- **No arbitrary code execution**: Does every package-processing path inspect only package files, nuspec metadata, and manifest JSON?
  - Not impacted. No package processing paths change.
- **Stable contracts**: Are `Elsa.PackageManifests` changes dependency-light, versioned, and separate from persistence/runtime internals?
  - Not impacted. No manifest contracts change.
- **Schema evolution**: Are schema versioning, extension metadata, compatibility behavior, and breaking-change rules documented?
  - Not impacted.
- **Immutable versions**: Does package-version handling preserve existing manifests and flag suspicious content changes?
  - Not impacted.
- **Approval separation**: Are validation, approval, and listing modeled as separate concerns?
  - Preserved. This feature only gates access to existing admin workflows.
- **Explicit sources**: Are package sources configured explicitly with include/exclude scope?
  - Not impacted.
- **Safe public API**: Are public responses limited to valid, approved, listed versions?
  - Preserved. Public endpoints remain anonymous and unchanged.
- **Debuggability**: Are sync runs, validation errors, indexing decisions, and suspicious changes persisted and inspectable?
  - Preserved. Dashboard inspectability remains available after login.
- **Modular monolith**: Does the design avoid distributed infrastructure unless justified?
  - Pass. The design stays inside the existing ASP.NET Core host.
- **Runtime Builder readiness**: Do APIs and manifests support package discovery, feature selection, settings schemas, and compatibility checks?
  - Not impacted.
- **Simplicity**: Are new abstractions, dependencies, and infrastructure justified by current requirements?
  - Pass. Uses existing framework authentication and existing admin key configuration.

## Project Structure

### Documentation (this feature)

```text
specs/004-admin-dashboard-auth/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)
<!--
  ACTION REQUIRED: Replace the placeholder tree below with the concrete layout
  for this feature. Delete unused options and expand the chosen structure with
  real paths (e.g., apps/admin, packages/something). The delivered plan must
  not include Option labels.
-->

```text
src/
├── Elsa.Catalog.Api/
│   ├── Authentication/
│   ├── Admin/
│   └── Program.cs
└── Elsa.Catalog.AdminUi/
    └── src/

tests/
└── Elsa.Catalog.Api.Tests/
    └── AdminDashboardAuthenticationTests.cs
```

**Structure Decision**: Keep the implementation in the existing API host because it already owns admin API authentication and serves the built dashboard assets. Add focused integration tests in the API test project.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | N/A | N/A |

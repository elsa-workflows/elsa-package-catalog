# Tasks: Server-Side Planning

**Input**: Design documents from `/specs/012-server-side-planning/`

## Phase 1: Setup

- [ ] T001 [P] Create planner core test file `tests/Elsa.Catalog.Core.Tests/BuilderPlannerTests.cs`
- [ ] T002 [P] Create planner API test file `tests/Elsa.Catalog.Api.Tests/BuilderPlanApiTests.cs`

## Phase 2: Foundation

- [ ] T003 Define planner models in `src/Elsa.Catalog.Core/Builder/Planner/BuilderPlannerModels.cs`
- [ ] T004 Implement deterministic planner service skeleton in `src/Elsa.Catalog.Core/Builder/Planner/BuilderPlannerService.cs`
- [ ] T005 Register planner service in `src/Elsa.Catalog.Api/Program.cs`

## Phase 3: User Story 1 - Resolve Builder Intent (Priority: P1)

- [ ] T006 [US1] Add dependency closure tests in `tests/Elsa.Catalog.Core.Tests/BuilderPlannerTests.cs`
- [ ] T007 [US1] Implement package and feature dependency closure in `src/Elsa.Catalog.Core/Builder/Planner/BuilderPlannerService.cs`
- [ ] T008 [US1] Add `POST /api/builder/plan` DTOs in `src/Elsa.Catalog.Api/Public/Builder/BuilderContracts.cs`
- [ ] T009 [US1] Add `POST /api/builder/plan` endpoint in `src/Elsa.Catalog.Api/Public/Builder/BuilderEndpoints.cs`

## Phase 4: User Story 2 - Shared Plan Across Resolve And Bundle (Priority: P1)

- [ ] T010 [US2] Add tests for matching plan/resolve/bundle findings in `tests/Elsa.Catalog.Core.Tests/BuilderPlannerTests.cs`
- [ ] T011 [US2] Integrate planner into compatibility resolve flow in `src/Elsa.Catalog.Api/Public/Builder/BuilderEndpoints.cs`
- [ ] T012 [US2] Integrate planner into bundle generation in `src/Elsa.Catalog.Core/Builder/BundleGenerationService.cs`

## Phase 5: User Story 3 - Frontend Presentation Only (Priority: P2)

- [ ] T013 [US3] Add API tests for resolved state and auto-added response shape in `tests/Elsa.Catalog.Api.Tests/BuilderPlanApiTests.cs`
- [ ] T014 [US3] Add workspace planner endpoint in `src/Elsa.Catalog.Api/Workspace/WorkspaceBuilderEndpoints.cs`
- [ ] T015 [US3] Document frontend migration notes in `specs/012-server-side-planning/quickstart.md`

## Phase 6: Polish

- [ ] T016 Add planner examples in `src/Elsa.Catalog.Api/Elsa.Catalog.Api.http`
- [ ] T017 Run `dotnet build Elsa.PackageCatalog.sln --no-restore` against `Elsa.PackageCatalog.sln`
- [ ] T018 Run `dotnet test Elsa.PackageCatalog.sln --no-build` against `Elsa.PackageCatalog.sln`

## Dependencies

- Foundation blocks all stories.
- US1 and US2 are MVP.

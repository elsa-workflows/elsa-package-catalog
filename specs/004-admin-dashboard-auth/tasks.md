# Tasks: Admin Dashboard Authentication

**Input**: Design documents from `/specs/004-admin-dashboard-auth/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Integration tests are required because this feature changes authentication and routing behavior.

**Organization**: Tasks are grouped by user story to preserve an independently testable MVP.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story the task supports
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm the existing API host remains the auth boundary.

- [X] T001 Review existing auth policy and static file ordering in `src/Elsa.Catalog.Api/Program.cs` and `src/Elsa.Catalog.Api/Authentication/`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Add reusable dashboard auth primitives before user story work.

- [X] T002 Add dashboard cookie auth defaults and admin-key validation helpers in `src/Elsa.Catalog.Api/Authentication/`
- [X] T003 Update admin authorization policy in `src/Elsa.Catalog.Api/Authentication/AdminAuthorization.cs` to accept API key or dashboard cookie

**Checkpoint**: Foundation ready - user story implementation can start.

---

## Phase 3: User Story 1 - Require Admin Login For Dashboard (Priority: P1) MVP

**Goal**: Anonymous users cannot load dashboard shell or assets.

**Independent Test**: Anonymous `/admin/overview` redirects to login and anonymous asset requests are not served.

### Tests for User Story 1

- [X] T004 [US1] Add anonymous dashboard access tests in `tests/Elsa.Catalog.Api.Tests/AdminDashboardAuthenticationTests.cs`

### Implementation for User Story 1

- [X] T005 [US1] Register dashboard cookie auth and place dashboard gating before static file serving in `src/Elsa.Catalog.Api/Program.cs`
- [X] T006 [US1] Implement dashboard path authorization middleware in `src/Elsa.Catalog.Api/Authentication/`

**Checkpoint**: Anonymous dashboard access is blocked while public endpoints remain public.

---

## Phase 4: User Story 2 - Sign In With Existing Admin Key (Priority: P1)

**Goal**: Admins can sign in once with the existing admin key and use the dashboard without browser-readable API key storage.

**Independent Test**: Valid login creates a session that authorizes admin API requests; invalid login does not.

### Tests for User Story 2

- [X] T007 [US2] Add login success/failure and cookie-authorized admin API tests in `tests/Elsa.Catalog.Api.Tests/AdminDashboardAuthenticationTests.cs`

### Implementation for User Story 2

- [X] T008 [US2] Implement minimal login endpoints in `src/Elsa.Catalog.Api/Authentication/`
- [X] T009 [US2] Wire login endpoints in `src/Elsa.Catalog.Api/Program.cs`

**Checkpoint**: Dashboard session login works and existing API-key clients still work.

---

## Phase 5: User Story 3 - Sign Out Of Dashboard Session (Priority: P2)

**Goal**: Admins can explicitly clear the dashboard session.

**Independent Test**: Logout clears the session and dashboard routes require login again.

### Tests for User Story 3

- [X] T010 [US3] Add logout test in `tests/Elsa.Catalog.Api.Tests/AdminDashboardAuthenticationTests.cs`

### Implementation for User Story 3

- [X] T011 [US3] Implement logout endpoint in `src/Elsa.Catalog.Api/Authentication/`

**Checkpoint**: Login and logout lifecycle works end to end.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Validate and document the completed security behavior.

- [X] T012 Run `dotnet test tests/Elsa.Catalog.Api.Tests/Elsa.Catalog.Api.Tests.csproj`
- [X] T013 Run quickstart smoke checks from `specs/004-admin-dashboard-auth/quickstart.md`
- [X] T014 Update task completion markers in `specs/004-admin-dashboard-auth/tasks.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup; blocks all stories.
- **User Story 1 and 2**: Depend on Foundational. Both are MVP priority; implement US1 first to close anonymous access, then US2 to restore authorized browser use.
- **User Story 3**: Depends on US2.
- **Polish**: Depends on all selected stories.

### Parallel Opportunities

- T004 and T007 touch the same test file and should be done sequentially.
- Implementation tasks in `Program.cs` should be sequential to preserve middleware order.
- No parallel worker split is recommended for this small security change.

## Implementation Strategy

1. Add tests for anonymous blocking and login behavior.
2. Add auth primitives and middleware.
3. Wire cookie auth into the existing admin policy.
4. Add logout.
5. Run the API test suite and smoke the deployed routes after push.

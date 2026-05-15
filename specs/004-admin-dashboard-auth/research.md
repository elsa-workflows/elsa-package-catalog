# Research: Admin Dashboard Authentication

## Decision: Reuse the existing admin API key to establish an HTTP-only dashboard cookie

**Rationale**: The production system already has an authenticated admin API backed by a configured API key. Reusing that secret lets the dashboard require auth immediately without adding a user database, OIDC setup, or role model.

**Alternatives considered**:

- **Keep only API key headers**: Rejected because the React dashboard would need the key in browser-readable configuration or manual request setup.
- **Put the dashboard behind platform auth only**: Useful later, but it would make local and container behavior diverge from the application security model.
- **Add OIDC now**: Rejected for MVP scope. It is the likely future long-term option, but not needed to stop anonymous access.

## Decision: Gate dashboard paths in the API host before serving static files

**Rationale**: The dashboard is deployed as static assets under `/admin`. `UseStaticFiles()` can serve files before endpoint authorization, so dashboard path access must be checked before static file serving.

**Alternatives considered**:

- **Protect only the fallback HTML endpoint**: Rejected because built JavaScript and CSS assets could still be fetched anonymously.
- **Move assets to a separate authenticated service**: Rejected as distributed infrastructure outside the current operational scope.

## Decision: Let admin API policy accept either API key or dashboard cookie

**Rationale**: Existing machine clients must keep using the API key header. Browser dashboard calls should use the HTTP-only cookie and avoid storing the key in frontend code.

**Alternatives considered**:

- **Require frontend API key env var**: Rejected because it exposes the credential to the browser bundle.
- **Create separate dashboard-only API endpoints**: Rejected as duplicate surface area.

## Decision: Keep login UI server-rendered and minimal

**Rationale**: The login page is a security boundary and should not require the protected React asset bundle to load. A small server-rendered form is enough for MVP.

**Alternatives considered**:

- **Build login into React app**: Rejected because loading the React app before authentication weakens the access boundary and complicates asset gating.

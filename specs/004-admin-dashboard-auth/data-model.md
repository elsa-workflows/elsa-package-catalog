# Data Model: Admin Dashboard Authentication

## Dashboard Session

Represents a successful browser admin login.

Fields:

- `scheme`: Dashboard cookie authentication scheme.
- `subject`: Stable admin identity claim, currently `admin-dashboard`.
- `issuedAt`: Session creation timestamp.
- `expiresAt`: Session expiration timestamp.

Rules:

- Stored only in an HTTP-only authentication cookie.
- Does not contain the admin API key.
- Grants access to dashboard routes and existing admin API policy.
- Can be cleared by logout.

## Admin Credential Submission

Represents the login form submission.

Fields:

- `apiKey`: Admin key entered by the operator.
- `returnUrl`: Optional local dashboard URL to return to after login.

Rules:

- Valid only when `apiKey` matches the configured admin API key.
- Invalid submissions must not create a session.
- `returnUrl` must resolve to a safe local `/admin` path and must not point to the login endpoint or an external host.

## Authenticated Admin Principal

Represents either API key or dashboard cookie authentication for admin authorization.

Fields:

- `nameIdentifier`: `api-key` for machine clients or `admin-dashboard` for browser sessions.
- `name`: Matching display name for diagnostics.
- `authenticationScheme`: API key or dashboard cookie.

Rules:

- Existing admin authorization policy accepts both schemes.
- Public endpoints do not require this principal.

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
- Uses an 8-hour sliding expiration.
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
- Failed submissions contribute to an in-memory per-client throttle.

## Login Throttle Entry

Represents transient failed-login state for one client.

Fields:

- `clientKey`: Non-persistent key derived from request client context.
- `failedAttemptCount`: Recent failed login count for the client.
- `retryAfter`: Earliest time another login attempt should be processed.

Rules:

- Stored in memory only.
- A throttle entry activates after 5 failed attempts in a 15-minute window.
- Active throttle entries require a 5-minute retry delay before another login
  attempt is processed.
- Successful login clears the client's throttle entry.
- Process restart clears all throttle entries.

## Authenticated Admin Principal

Represents either API key or dashboard cookie authentication for admin authorization.

Fields:

- `nameIdentifier`: `api-key` for machine clients or `admin-dashboard` for browser sessions.
- `name`: Matching display name for diagnostics.
- `authenticationScheme`: API key or dashboard cookie.

Rules:

- Existing admin authorization policy accepts both schemes.
- Public endpoints do not require this principal.

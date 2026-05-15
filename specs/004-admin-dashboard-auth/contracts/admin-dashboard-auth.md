# Contract: Admin Dashboard Authentication

## GET /admin/login

Returns a minimal HTML login form.

Query parameters:

- `returnUrl` optional local admin path.

Expected behavior:

- Anonymous callers receive `200 OK` with login form HTML.
- Already-authenticated dashboard sessions redirect to the safe `returnUrl` or `/admin/overview`.

## POST /admin/login

Accepts form data:

- `apiKey`: required admin key.
- `returnUrl`: optional local admin path.

Expected behavior:

- Valid key: `302 Found` to safe `returnUrl` or `/admin/overview`, with an HTTP-only dashboard auth cookie.
- Invalid key: `401 Unauthorized` with login form HTML and no session cookie.
- Missing configured server key: `401 Unauthorized` and no session cookie.
- Unsafe `returnUrl`: ignored in favor of `/admin/overview`.

## POST /admin/logout

Expected behavior:

- Clears dashboard auth cookie.
- Redirects to `/admin/login`.

## GET /admin/{path}

Expected behavior:

- Authenticated dashboard session or valid API key: serves dashboard content.
- Anonymous browser navigation: redirects to `/admin/login?returnUrl=/admin/{path}`.
- Anonymous non-HTML/static requests: returns `401 Unauthorized`.

## /api/admin/*

Expected behavior:

- Valid `X-Api-Key` header: authorized, unchanged from current behavior.
- Valid dashboard auth cookie: authorized.
- Anonymous request: `401 Unauthorized`.

## Public Endpoints

`/health`, `/`, `/api/packages`, `/api/features`, and compatibility/public package routes remain anonymous.

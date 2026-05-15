# Quickstart: Admin Dashboard Authentication

## Local Verification

1. Start the API host with `Authentication:ApiKey` configured.
2. Open `/admin/overview` without credentials.
3. Confirm the request redirects to `/admin/login`.
4. Submit an invalid key and confirm login is rejected.
5. Submit the configured admin key and confirm the browser redirects to `/admin/overview`.
6. Confirm dashboard admin API calls succeed using the session cookie.
7. Call `/api/admin/sources` with the `X-Api-Key` header and confirm existing API clients still work.
8. Open `/health` and a public catalog endpoint without credentials and confirm they remain public.
9. Submit `/admin/logout` and confirm `/admin/overview` requires login again.

## Automated Verification

Run:

```sh
dotnet test tests/Elsa.Catalog.Api.Tests/Elsa.Catalog.Api.Tests.csproj
```

## Deployment Smoke

After deployment:

1. `GET https://<app>/admin/overview` should redirect to `/admin/login`.
2. `GET https://<app>/admin/assets/<asset>` without a cookie should not return the asset.
3. `GET https://<app>/health` should return `200 OK`.

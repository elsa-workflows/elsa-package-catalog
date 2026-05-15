# Elsa Catalog Admin UI

Lightweight operational dashboard for Elsa Package Catalog administrators.

## Development

```bash
npm install
npm run dev
```

Set `VITE_CATALOG_API_PROXY_TARGET` and `VITE_ADMIN_API_KEY` in a local `.env`
file. The browser client uses relative `/api` requests by default so the Vite
dev proxy can avoid CORS requirements. Do not commit local secrets.

## Verification

```bash
npm test
npm run typecheck
npm run build
```

The MVP must expose only Overview, Sources, Packages, and Sync Runs. It must not
include Settings, package identity approval controls, hard-delete source
controls, realtime streaming logs, or manifest editing.

## Deployment

The production API container builds this app and serves it from `/admin`. Vite is
configured with `/admin/` as its asset base path, and browser API calls remain
same-origin `/api` requests.

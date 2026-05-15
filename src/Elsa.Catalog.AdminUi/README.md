# Elsa Catalog Admin UI

Lightweight operational dashboard for Elsa Package Catalog administrators.

## Development

```bash
npm install
npm run dev
```

Set `VITE_CATALOG_API_URL` and `VITE_ADMIN_API_KEY` in a local `.env` file. Do
not commit local secrets.

## Verification

```bash
npm test
npm run typecheck
npm run build
```

The MVP must expose only Overview, Sources, Packages, and Sync Runs. It must not
include Settings, package identity approval controls, hard-delete source
controls, realtime streaming logs, or manifest editing.

# Elsa Package Catalog

> Deprecated: active development has moved to the Elsa Platform repository.
> Use https://github.com/elsa-workflows/elsa-platform for Package Catalog,
> Package Manifest, Runtime Builder, and Deployment Platform work.

Elsa Package Catalog is a modular ASP.NET Core catalog API for approved Elsa professional runtime packages. It indexes configured NuGet package sources, extracts `elsa-package.json` manifests without loading package assemblies, validates manifests, stores sync diagnostics, and exposes public discovery APIs for future Runtime Builder tooling.

The solution also contains `Elsa.PackageManifests`, the shared manifest wire-contract package used by manifest generation, catalog ingestion, runtime validation, and builder clients.

## Local Development

```bash
dotnet restore Elsa.PackageCatalog.sln
dotnet test Elsa.PackageCatalog.sln
dotnet run --project src/Elsa.Catalog.Api
```

The development catalog database uses SQLite at `elsa-catalog-dev.db` unless overridden with `ConnectionStrings:Catalog`.

## Admin API Key

Admin APIs require the `X-Api-Key` header. For local development the default key is `local-dev-key`.

```bash
curl -H "X-Api-Key: local-dev-key" http://localhost:5000/api/admin/sources
```

## Sync Safety

Package processing is manifest-first. The catalog reads NuGet package files, nuspec metadata, and manifest JSON only. It must not load or execute assemblies from indexed packages.

Package versions are treated as immutable. If the same package ID/version later produces different manifest content, the catalog marks the version suspicious instead of replacing it silently.

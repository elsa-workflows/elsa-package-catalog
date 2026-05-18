# Quickstart: Runtime Image Metadata API

## Scenario 1: Builder Catalog Returns Images

```http
GET /api/builder/catalog
```

Expected:

- Response includes `images`.
- Known slugs are present: `elsa-pro-server`, `elsa-pro-studio`, `elsa-pro-combined`.
- Each image includes image reference, default tag, ports, capabilities, env vars, and deployment hints.

## Scenario 2: Bundle Generation Uses Image Metadata

Generate a bundle for each known image slug.

Expected:

- Generated Compose output uses backend-owned image reference, tag, container name, and ports.
- Environment defaults come from backend metadata.
- Studio companion behavior follows backend deployment hints.

## Scenario 3: Unknown Image Is Rejected

Submit `image.slug = "elsa-pro-unknown"` to bundle generation.

Expected:

- Response includes an error finding.
- No generated files are returned for blocking image errors.

## Scenario 4: Metadata Validation

Run runtime image catalog validation tests.

Expected:

- Duplicate slugs fail.
- Missing image references fail.
- Invalid default tags fail.
- Duplicate env var names fail.
- Broken companion references fail.

## Validation Commands

```bash
dotnet build Elsa.PackageCatalog.sln --no-restore
dotnet test Elsa.PackageCatalog.sln --no-build
```

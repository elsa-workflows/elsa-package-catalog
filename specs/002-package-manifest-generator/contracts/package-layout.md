# Contract: NuGet Package Layout

## Consuming Package Input

A package project opts in with:

```xml
<PackageReference Include="Elsa.PackageManifest.Generator" Version="x.y.z" PrivateAssets="all" />
```

The generator package supplies:

- MSBuild props/targets.
- MSBuild task assembly.
- Optional source-only manifest hint attributes.
- Pack integration for the generated manifest.

## Generated Intermediate Output

Single-target default:

```text
obj/{configuration}/{targetframework}/elsa-package.json
```

Multi-target default:

```text
obj/{configuration}/{targetframework}/elsa-package.json
```

Per-target intermediate manifests may be generated for comparison, but package
inclusion produces one canonical root manifest by default.

## NuGet Package Output

Default package path:

```text
elsa-package.json
```

Rules:

- The root package path is canonical.
- The package contains exactly one root `elsa-package.json` by default.
- Alternate paths are allowed only through explicit configuration.
- Generated manifests larger than 1 MB fail validation before package inclusion.
- The manifest package ID/version must match NuGet package metadata.

## Generator Package Layout

Recommended generated `.nupkg` shape for `Elsa.PackageManifest.Generator`:

```text
build/
├── Elsa.PackageManifest.Generator.props
└── Elsa.PackageManifest.Generator.targets
buildTransitive/
├── Elsa.PackageManifest.Generator.props
└── Elsa.PackageManifest.Generator.targets
tasks/
└── Elsa.PackageManifest.Generator.MSBuild.dll
contentFiles/
└── cs/
    └── any/
        └── Elsa.PackageManifest.Generator.Hints/
            ├── ManifestSettingAttribute.cs
            ├── ManifestIgnoreAttribute.cs
            └── ManifestExtensionAttribute.cs
```

The exact final packing layout may vary if MSBuild task loading requires a
different task asset path, but the package author contract must stay one
private package reference with optional source-only manifest hints and automatic manifest
inclusion.

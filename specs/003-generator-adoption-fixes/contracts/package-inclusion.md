# Contract: Multi-Target Package Manifest Inclusion

## Purpose

Define default package inclusion behavior for generated `elsa-package.json` in
single-target and multi-target consuming projects.

## Default Consumer Contract

Consumers use only:

```xml
<PackageReference Include="Elsa.PackageManifest.Generator" Version="x.y.z" PrivateAssets="all" />
```

No custom manifest item, custom pack target, or
`TargetsForTfmSpecificContentInPackage` workaround is required.

## Single-Target Projects

- Generate the intermediate manifest under the active target framework
  intermediate output path.
- Include one package entry at `$(ElsaPackageManifestPackagePath)`, defaulting to
  `elsa-package.json`.

## Multi-Target Projects

- Per-target manifests may be generated under each target framework's
  intermediate output path.
- Equivalent manifest surfaces choose the first declared target framework as the
  canonical source.
- Package inclusion creates exactly one package entry at
  `$(ElsaPackageManifestPackagePath)`, defaulting to root `elsa-package.json`.
- Divergent target-framework surfaces are diagnosed according to the configured
  severity policy unless explicitly allowed.

## Direct Pack

Direct `dotnet pack` must generate and include the canonical manifest when a
separate explicit build was not run first.

## Custom Package Path

When `ElsaPackageManifestPackagePath` is configured, exactly one manifest is
included at that package path by default.

## Acceptance Tests

- Single-target pack contains exactly one configured manifest package entry.
- Multi-target pack with equivalent surfaces contains exactly one root
  `elsa-package.json`.
- The canonical manifest source is the first declared target framework.
- Direct pack includes the canonical manifest without requiring a prior build.
- Consumer-side `TargetsForTfmSpecificContentInPackage` workarounds are not
  needed.

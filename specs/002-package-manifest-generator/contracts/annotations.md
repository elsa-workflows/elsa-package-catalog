# Contract: Source-Only Annotations

## Namespace

Source-only annotation attributes are emitted into:

```csharp
namespace Elsa.PackageManifest.Generator.Annotations;
```

They are compile-time generator inputs only. They are not part of the
`Elsa.PackageManifests` wire contract and must not create runtime dependencies
in consuming packages.

## ElsaFeatureAttribute

Applies to classes.

Supported metadata:

- `Id`
- `DisplayName`
- `Category`
- `Description`
- `Advanced`
- `Experimental`

Purpose:

- Explicitly include a feature class.
- Enrich or override inferred feature metadata.
- Resolve ambiguity when convention discovery finds a type.

## FeatureSettingAttribute

Applies to properties.

Supported metadata:

- `Name`
- `DisplayName`
- `Description`
- `Category`
- `Group`
- `Required`
- `DefaultValue`
- `EnvironmentVariable`
- `UiHint`
- `Secret`
- `Sensitive`
- `RestartRequired`
- `Advanced`
- `Experimental`

Purpose:

- Include or enrich configurable feature settings.
- Override display and configuration metadata that cannot be inferred reliably.

## ManifestIgnoreAttribute

Applies to classes or properties.

Purpose:

- Exclude feature classes or feature setting properties from manifest generation.

## ManifestExtensionAttribute

Applies to classes or properties. Attribute-based extension metadata is limited
to simple string key/value pairs.

Supported metadata:

- `Key`
- `Value`

Purpose:

- Supply small extension metadata values.
- Rich extension payloads must be supplied through `elsa-package.overrides.json`.

## CompatibilityAttribute

Applies to assemblies or feature classes.

Supported metadata:

- `ElsaVersionRange`
- `DockerImageVersionRange`
- `RuntimeCapabilities`

Purpose:

- Supply package or feature compatibility metadata that cannot be inferred.

## RequiresFeatureAttribute

Applies to feature classes.

Supported metadata:

- `FeatureId`

Purpose:

- Declare feature dependencies.

## ConflictsWithFeatureAttribute

Applies to feature classes.

Supported metadata:

- `FeatureId`

Purpose:

- Declare feature conflicts.

## Rules

- Attribute values are merged after inferred metadata and XML documentation.
- Override file values still win over attribute values.
- Attributes must not duplicate manifest DTOs or replace the
  `Elsa.PackageManifests` contract.
- Attribute values must be representable without executing package code.

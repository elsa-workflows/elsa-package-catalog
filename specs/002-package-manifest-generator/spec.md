# Feature Specification: Elsa Package Manifest Generator

**Feature Branch**: `002-package-manifest-generator`

**Created**: 2026-05-14

**Status**: Draft

**Input**: User description: "Create a specification for a build-time package called Elsa.PackageManifest.Generator."

## Overview

Elsa Package Manifest Generator is a build-time package referenced by class library projects that publish NuGet packages for the Elsa professional runtime ecosystem. During build and pack, it automatically produces an `elsa-package.json` manifest that describes the NuGet package, its exposed CShells features, configurable feature settings, compatibility metadata, documentation metadata, and other catalog-relevant information.

The generated manifest is the distribution contract consumed later by Elsa Package Catalog, Elsa Runtime Builder, professional Elsa Docker images, and future runtime validation tools. The generator must emit manifests using the shared `Elsa.PackageManifests` wire contract package and must not define or serialize a separate manifest model.

The first version uses an MSBuild-integrated generator because the output is an external artifact that must be created during build or pack and included in the NuGet package. The generator inspects compiled assembly metadata, project and NuGet metadata, XML documentation files, referenced metadata needed for type identification, optional source annotations, and an optional override file. It must not execute package code or invoke feature constructors.

## Goals

- Automatically generate `elsa-package.json` during build or pack for participating package projects.
- Include the generated manifest in the resulting NuGet package without requiring manual item configuration.
- Reuse `Elsa.PackageManifests` as the canonical manifest contract and validation source.
- Discover package metadata from project and NuGet metadata wherever reliable.
- Discover CShells feature classes exposed by the package project.
- Discover feature settings from configurable feature properties.
- Extract XML documentation comments for feature and setting descriptions when available.
- Generate JSON Schema metadata for feature settings.
- Allow explicit source annotations and override files for metadata that cannot be inferred safely.
- Validate the final manifest against the versioned schema from `Elsa.PackageManifests`.
- Support multi-targeted package projects predictably.
- Keep generation deterministic, CI-friendly, quiet by default, and safe.
- Minimize manual manifest maintenance for package authors.

## Non-Goals

- Implementing Elsa Package Catalog ingestion or APIs.
- Building the Runtime Builder UI.
- Installing packages through Nuplane.
- Creating or configuring professional Docker images.
- Runtime dependency resolution.
- Runtime validation execution.
- Sigil license validation.
- Executing feature code or evaluating runtime behavior.
- Generating deployment bundles.
- Replacing `Elsa.PackageManifests` with generator-owned DTOs.
- Requiring analyzers or Roslyn source generators in the first version.
- Building a generalized plugin system for custom discovery.

## Personas

- **Package Author**: Maintains an Elsa professional extension package and wants a manifest generated during normal build and pack without hand-writing JSON.
- **Runtime Builder Developer**: Relies on complete and stable manifests to render package features, settings, documentation, and compatibility information.
- **Catalog Operator**: Expects packages to contain valid manifests that can be indexed and validated without executing package code.
- **Build Engineer**: Needs deterministic CI behavior, clear diagnostics, and predictable failure modes.
- **Manifest Contract Maintainer**: Evolves `Elsa.PackageManifests` and needs the generator to follow the shared wire contract without drift.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Generate Manifest Automatically (Priority: P1)

A package author adds a private build-time reference to `Elsa.PackageManifest.Generator` and receives a generated `elsa-package.json` during build or pack.

**Why this priority**: Automatic generation is the core value of the package and removes manual manifest maintenance.

**Independent Test**: Create a class library package project with package metadata, reference the generator package, build and pack it, and verify that a valid manifest is generated under the intermediate output path and included in the produced NuGet package.

**Acceptance Scenarios**:

1. **Given** a package project references the generator with private assets, **When** the project is built, **Then** the generator creates `elsa-package.json` in the configured intermediate output location.
2. **Given** the same package project is packed, **When** the package is inspected, **Then** the NuGet package contains one canonical `elsa-package.json` at the package root.
3. **Given** manifest generation is disabled by configuration, **When** the project is built or packed, **Then** no manifest is generated or included.

---

### User Story 2 - Discover Features and Settings (Priority: P1)

A package author exposes CShells feature classes and public configurable settings, and the generator describes them in the manifest without requiring duplicate JSON declarations.

**Why this priority**: Feature and setting discovery is the manifest content most valuable to Runtime Builder and Docker image configuration workflows.

**Independent Test**: Build a package containing feature classes with configurable properties, then verify that the generated manifest includes feature identities, display metadata, settings, type metadata, schema metadata, validation constraints, and descriptions where available.

**Acceptance Scenarios**:

1. **Given** a public feature class derives from a known CShells feature base class or implements a known feature interface, **When** generation runs, **Then** the feature is included in the manifest.
2. **Given** a feature class uses an explicit feature annotation, **When** generation runs, **Then** annotation values enrich or override inferred feature metadata.
3. **Given** a feature setting property is public and settable, **When** generation runs, **Then** the property is included as a setting unless explicitly ignored.
4. **Given** a property is static, read-only, computed-only, or ignored, **When** generation runs, **Then** it is excluded unless explicitly included by supported metadata.

---

### User Story 3 - Enrich Metadata from Documentation and Overrides (Priority: P2)

A package author can use XML documentation, source annotations, and an override file to supply names, descriptions, documentation links, compatibility ranges, and metadata that inference cannot know.

**Why this priority**: Inference alone cannot produce high-quality catalog and UI metadata.

**Independent Test**: Build a project with XML documentation, feature and setting annotations, and `elsa-package.overrides.json`, then verify that final manifest fields follow the documented merge order and conflict rules.

**Acceptance Scenarios**:

1. **Given** XML documentation contains feature and setting summaries, **When** generation runs, **Then** those summaries populate missing descriptions.
2. **Given** annotations provide display names or categories, **When** generation runs, **Then** those values override inferred defaults for the annotated target.
3. **Given** the override file provides package, feature, or setting metadata, **When** generation runs, **Then** override values take precedence over inferred, XML, and annotation values according to merge rules.

---

### User Story 4 - Validate and Diagnose Build Output (Priority: P2)

A build engineer receives clear diagnostics when a manifest cannot be generated or validated, and can configure whether validation issues fail the build.

**Why this priority**: Invalid manifests must be caught before packages reach the catalog, while teams still need controlled adoption paths.

**Independent Test**: Generate manifests with missing recommended metadata, invalid required metadata, unsupported setting types, and invalid override JSON, then verify build diagnostics and configured failure behavior.

**Acceptance Scenarios**:

1. **Given** the final manifest violates the required manifest schema, **When** default validation runs, **Then** the build fails with actionable error diagnostics.
2. **Given** recommended metadata such as descriptions is missing, **When** default validation runs, **Then** warnings are emitted without noisy output.
3. **Given** strict mode or fail-on-warnings is enabled, **When** recommended metadata is missing, **Then** warnings can fail the build.

---

### User Story 5 - Handle Multi-Targeting Predictably (Priority: P2)

A package author multi-targets frameworks and still receives one canonical package manifest unless target-specific differences are explicitly configured.

**Why this priority**: NuGet packages should not contain duplicate or contradictory root manifests.

**Independent Test**: Build multi-targeted projects with identical feature surfaces, different feature surfaces, and explicit target-specific configuration, then verify canonical manifest behavior and diagnostics.

**Acceptance Scenarios**:

1. **Given** all target frameworks produce the same manifest-relevant feature surface, **When** packing, **Then** the NuGet package contains one canonical root `elsa-package.json`.
2. **Given** target frameworks produce different feature or setting surfaces, **When** generation runs, **Then** the build warns or fails according to configured severity unless target-specific differences are explicitly allowed.
3. **Given** generation runs once per target framework internally, **When** package inclusion occurs, **Then** duplicate package-level manifests are avoided.

### Edge Cases

- The consuming project does not produce an assembly.
- The compiled assembly cannot be inspected.
- XML documentation generation is disabled or the XML documentation file is missing.
- XML documentation exists but contains no summary for a discovered feature or setting.
- A feature class is internal, abstract, generic, nested, or otherwise not intended for runtime exposure.
- A feature class matches convention-based discovery but is explicitly ignored.
- A class has both inferred and explicit feature metadata with conflicting values.
- A setting property has an unsupported or ambiguous type.
- A setting property uses nullable reference metadata that is unavailable or inconsistent with annotations.
- Default values cannot be determined safely without executing constructors.
- Multiple target frameworks produce different feature surfaces.
- The override file is missing, malformed, uses unknown fields, or references nonexistent features or settings.
- Generated manifest output already exists from a previous build.
- Package metadata such as description, repository URL, license, or readme is absent.
- Validation schema version from `Elsa.PackageManifests` is unsupported or unavailable.
- The package is packed without running a normal build first.
- Concurrent builds write to separate intermediate output paths.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The generator MUST be consumable by class library projects through a package reference named `Elsa.PackageManifest.Generator` with `PrivateAssets="all"`.
- **FR-002**: The consuming project MUST NOT need to manually add build targets, package items, or hand-maintained manifest files for the default workflow.
- **FR-003**: The generator MUST run automatically during build or pack when manifest generation is enabled.
- **FR-004**: The generator MUST write the generated manifest file as `elsa-package.json`.
- **FR-005**: The default intermediate output location MUST be under the project intermediate output path for the active configuration and target framework.
- **FR-006**: The generated NuGet package MUST include a single canonical `elsa-package.json` at the package root by default.
- **FR-007**: The generator MUST support disabling generation entirely through project configuration.
- **FR-008**: The generator MUST emit manifests using the `Elsa.PackageManifests` contract and MUST NOT define an independent wire model for generated JSON.
- **FR-009**: The generator MUST validate the final manifest against the versioned schema supplied by `Elsa.PackageManifests`.
- **FR-010**: Validation errors MUST fail the build by default.
- **FR-011**: Missing recommended metadata SHOULD produce warnings by default rather than errors.
- **FR-012**: Validation severity MUST be configurable so adopters can choose warning-only, error-on-validation, strict, and fail-on-warnings behavior.
- **FR-013**: Generation MUST be deterministic for the same inputs, including stable ordering of packages, features, settings, schema properties, diagnostics, and serialized JSON.
- **FR-014**: Generation MUST be safe for CI and MUST NOT rely on machine-specific absolute paths in the manifest unless explicitly provided as metadata.
- **FR-015**: The generator MUST inspect compiled assembly metadata to discover manifest-relevant types.
- **FR-016**: The generator MUST inspect project and NuGet metadata for package-level manifest fields where available.
- **FR-017**: Package metadata inference MUST consider package ID, version, title, description, authors, repository URL, project URL, tags, license expression, readme file, and target frameworks.
- **FR-018**: The generator MUST read XML documentation files when available.
- **FR-019**: The generator MUST continue generation when XML documentation is unavailable, subject to configured diagnostics for missing descriptions.
- **FR-020**: The generator MUST support an optional override file named `elsa-package.overrides.json` by default.
- **FR-021**: The generator MUST support configuring a custom override file path.
- **FR-022**: The override file MUST be optional.
- **FR-023**: The override file MUST support package metadata, documentation metadata, icon metadata, tags, compatibility metadata, license metadata, feature metadata overrides, setting metadata overrides, dependencies, conflicts, required capabilities, and extension metadata.
- **FR-024**: The final manifest MUST be produced by merging inferred metadata, XML documentation, source annotations, and override file values in that order.
- **FR-025**: Later merge sources MUST take precedence over earlier merge sources for scalar values.
- **FR-026**: Collection merge behavior MUST be deterministic and documented per collection type.
- **FR-027**: The generator MUST report override entries that reference nonexistent features or settings.
- **FR-028**: The generator MUST discover CShells feature classes from the project assembly.
- **FR-029**: A feature class MUST be discoverable when it derives from a known CShells feature base class.
- **FR-030**: A feature class MUST be discoverable when it implements a known CShells feature interface.
- **FR-031**: A feature class MUST be discoverable when it has an explicit feature annotation.
- **FR-032**: Explicit feature annotations MUST be preferred where convention-based discovery is ambiguous.
- **FR-033**: Abstract feature classes MUST NOT be included as exposed features by default.
- **FR-034**: Generic feature type definitions MUST NOT be included as exposed features by default.
- **FR-035**: Internal feature classes SHOULD be excluded by default unless explicitly included by supported metadata.
- **FR-036**: Feature metadata MUST include feature ID, CLR type name, display name, description, category, settings, dependencies, conflicts, required capabilities, advanced flag, experimental flag, and extension metadata when available.
- **FR-037**: Feature IDs MUST be stable and deterministic.
- **FR-038**: When no explicit feature ID is provided, the generator MUST infer a feature ID from stable package and type metadata.
- **FR-039**: The generator MUST discover feature settings from public configurable properties on discovered feature classes.
- **FR-040**: Public instance properties with public setters MUST be included as settings by default.
- **FR-041**: Static properties MUST be excluded.
- **FR-042**: Read-only or computed properties MUST be excluded unless explicitly included by supported metadata.
- **FR-043**: Settings or feature members marked with an ignore annotation MUST be excluded.
- **FR-044**: Explicit setting annotations MUST be able to enrich or override inferred setting metadata.
- **FR-045**: The generator MUST support nullable value type metadata.
- **FR-046**: The generator SHOULD use nullable reference type metadata when available to infer required and nullable behavior.
- **FR-047**: The generator MUST support common validation annotations for settings.
- **FR-048**: Setting metadata MUST include name, CLR type, JSON type, required flag, nullable flag, default value when safely discoverable, display name, description, category or group, validation constraints, enum values, secret flag, sensitive flag, restart-required flag, environment variable mapping, UI hints, advanced flag, experimental flag, and extension metadata where available.
- **FR-049**: The generator MUST NOT execute feature constructors to discover default values.
- **FR-050**: The generator MAY include default values only when they are available from compile-time constants, attributes, override metadata, or other non-executing metadata.
- **FR-051**: Unsupported setting types MUST produce diagnostics whose severity is configurable.
- **FR-052**: The generator MUST map supported setting types to JSON Schema metadata.
- **FR-053**: String settings MUST map to JSON string schema.
- **FR-054**: Boolean settings MUST map to JSON boolean schema.
- **FR-055**: Integral numeric settings MUST map to JSON integer schema.
- **FR-056**: Floating point and decimal settings MUST map to JSON number schema.
- **FR-057**: Enum settings MUST map to string enum schema with deterministic enum value ordering.
- **FR-058**: Time duration settings MUST map to string schema with duration metadata.
- **FR-059**: Date and time settings MUST map to string schema with date-time metadata.
- **FR-060**: URI settings MUST map to string schema with URI metadata.
- **FR-061**: Arrays and list settings MUST map to array schema.
- **FR-062**: Dictionary settings MUST map to object schema with additional property metadata.
- **FR-063**: Complex object settings MUST map to object schema when they can be described safely from metadata.
- **FR-064**: Nullable settings MUST be represented consistently with the shared manifest contract and schema version.
- **FR-065**: XML documentation class summaries SHOULD populate feature descriptions when no higher-priority description is supplied.
- **FR-066**: XML documentation property summaries SHOULD populate setting descriptions when no higher-priority description is supplied.
- **FR-067**: XML documentation remarks MAY be included where the manifest contract supports extended documentation metadata.
- **FR-068**: XML documentation example tags MAY be included where the manifest contract supports examples.
- **FR-069**: Source annotations MUST be lightweight metadata annotations used only as generator inputs.
- **FR-070**: Source annotations MUST NOT replace or duplicate the shared manifest wire contract.
- **FR-071**: The first version SHOULD support feature, setting, ignore, extension, compatibility, dependency, conflict, required-feature, and conflicts-with annotations where useful.
- **FR-072**: The generator MUST produce clear diagnostics for discovered feature count, generated manifest path, missing XML documentation, invalid settings, unsupported property types, schema validation errors, and package inclusion.
- **FR-073**: Default diagnostics MUST avoid noisy per-property success logs.
- **FR-074**: The generator MUST support multi-targeted projects.
- **FR-075**: Multi-targeted projects MUST produce one canonical package-level manifest by default.
- **FR-076**: If target frameworks produce different manifest-relevant feature surfaces, the generator MUST warn or fail according to configured severity unless explicitly configured to allow target-specific differences.
- **FR-077**: The generator MUST avoid duplicate package-level manifest inclusion when pack runs for multiple target frameworks.
- **FR-078**: The generator MUST NOT execute package code.
- **FR-079**: The generator MUST avoid invoking constructors where possible.
- **FR-080**: The generator MUST use metadata inspection rather than runtime behavior for discovery.
- **FR-081**: The generator MUST work in CI without requiring interactive prompts.
- **FR-082**: The generator SHOULD complete quickly enough for normal package builds and SHOULD avoid repeated expensive inspection when inputs are unchanged.
- **FR-083**: Generated JSON MUST be stable enough for meaningful diffs and reproducible package builds.
- **FR-084**: Pack behavior MUST work when packing directly, even if the manifest was not generated by a separate explicit build command first.

### Key Entities *(include if feature involves data)*

- **Elsa Package Manifest**: The generated package-level JSON document. It contains package identity, metadata, features, compatibility, documentation, dependencies, conflicts, license information, schema version, and extension metadata.
- **Package Metadata**: Values inferred from project and NuGet metadata, including package ID, version, title, description, authors, repository URL, project URL, tags, license, readme, and target frameworks.
- **CShells Feature**: A project assembly type that exposes a runtime feature and can declare configurable settings.
- **Feature Setting**: A configurable public property on a discovered feature class, represented with type information, validation constraints, UI hints, environment mapping, sensitivity, and JSON Schema metadata.
- **XML Documentation Entry**: Documentation comments associated with a feature class or setting property, used as human-readable manifest metadata.
- **Source Annotation**: A lightweight attribute applied in package source code to enrich or override generator inference.
- **Override File**: Optional author-maintained JSON metadata file merged into the final manifest for information that cannot be inferred reliably.
- **Generated Settings Schema**: JSON Schema metadata describing each feature setting's shape, constraints, enum values, nullability, and supported UI interpretation.
- **Validation Result**: Structured outcome from validating the final manifest against the versioned schema and recommended metadata rules.

## Build Integration Design

The default integration is an MSBuild task distributed by `Elsa.PackageManifest.Generator`. A separate `Elsa.PackageManifest.Generator.MSBuild` package may be used internally if it simplifies packaging, but package authors should reference only `Elsa.PackageManifest.Generator` for the standard experience.

The generator runs after compilation has produced the project assembly and XML documentation, and before pack finalizes package contents. Pack must trigger generation when needed so a direct pack command produces the manifest.

The generated intermediate file path defaults to a target-framework-specific intermediate output directory such as `obj/{configuration}/{targetframework}/elsa-package.json`. The final NuGet package path is the package root: `elsa-package.json`. The root path is preferred because it is the simplest stable location for catalog ingestion and runtime tooling to discover.

The architecture decision for the first version is:

- **MSBuild task**: Required for MVP. It can inspect compiled assemblies, XML documentation, project metadata, NuGet metadata, references, and override files, and can include the resulting external artifact in the package.
- **Roslyn source generator or analyzer**: Deferred. It may later provide authoring diagnostics or generated strongly typed helpers, but it is not required to create the package artifact.
- **Hybrid approach**: Allowed later. The first version should not depend on a source generator for correctness.

Suggested package boundaries:

- `Elsa.PackageManifest.Generator`: Public package referenced by package authors; brings in build assets, annotations if exposed, and task assets.
- `Elsa.PackageManifest.Generator.Core`: Optional internal/shared library for generation logic if it removes meaningful duplication between task, tests, or future analyzers.
- `Elsa.PackageManifest.Generator.MSBuild`: Optional packaging/task assembly if separating MSBuild assets keeps the public package cleaner.
- `Elsa.PackageManifests`: Required shared contract package for manifest DTOs, schema resources, validation behavior, and serialization rules.

## Manifest Generation Flow

1. Resolve generator settings from project properties and defaults.
2. Resolve package metadata from project and NuGet properties.
3. Locate compiled assembly, XML documentation file, referenced assemblies needed for type identification, and optional override file.
4. Inspect compiled assembly metadata without executing package code.
5. Discover CShells feature classes using conventions and explicit annotations.
6. Discover configurable settings from feature properties.
7. Map settings to manifest metadata and generated JSON Schema metadata.
8. Extract XML documentation summaries, remarks, and examples where available.
9. Apply source annotation metadata.
10. Apply override file metadata.
11. Build the final `Elsa.PackageManifests` contract object.
12. Validate the final manifest against the versioned schema and recommended metadata rules.
13. Write deterministic `elsa-package.json` to the configured intermediate output path.
14. Include the generated file once in the NuGet package at the root path.
15. Emit concise diagnostics and fail or warn according to configured severity.

## Feature Discovery Rules

Feature discovery supports both convention-based and explicit discovery.

Convention-based discovery includes concrete exposed types that derive from a known CShells feature base class or implement a known CShells feature interface. Explicit discovery includes types annotated as Elsa features. Explicit metadata is preferred when convention-based discovery would be ambiguous.

Default inclusion rules:

- Include concrete public feature classes that match a known CShells base class, known feature interface, or explicit feature annotation.
- Exclude abstract classes, generic type definitions, static classes, and ignored classes.
- Exclude internal classes unless explicitly included.
- Use stable type identity metadata for CLR type names.
- Infer feature ID from explicit metadata first, then from stable package and type metadata.
- Infer display name from explicit metadata, XML documentation title-equivalent metadata when available, or readable type name.
- Infer category only from explicit metadata or overrides; do not invent categories from namespace segments unless later explicitly approved.

## Feature Setting Discovery Rules

Settings are discovered from public configurable properties on discovered feature classes.

Default inclusion rules:

- Include public instance properties with public setters.
- Exclude static properties.
- Exclude read-only or computed-only properties unless explicitly included.
- Exclude indexer properties.
- Exclude properties marked with ignore metadata.
- Include explicitly annotated properties when they are safe to describe.
- Preserve deterministic ordering by declared metadata order when available, then by property name.

Default required and nullable behavior:

- Non-nullable value types are required unless a default value or explicit optional metadata is supplied.
- Nullable value types are optional and nullable.
- Nullable reference type metadata is used when available.
- When reference nullability cannot be determined, settings are treated as optional unless explicit metadata says otherwise.

## XML Documentation Extraction

The generator reads XML documentation when present and maps documentation entries to discovered types and properties.

Extraction behavior:

- Class `<summary>` populates feature description when no source annotation or override description is supplied.
- Property `<summary>` populates setting description when no source annotation or override description is supplied.
- `<remarks>` may populate extended documentation fields when supported by the manifest contract.
- `<example>` may populate examples when supported by the manifest contract.
- Missing XML documentation does not stop generation by default.
- Missing descriptions generate warnings only when configured by recommended metadata validation or strict mode.

## Attribute Model

Attributes are optional source annotations for generation input only. They enrich inference and reduce override-file noise, but the generated JSON still uses `Elsa.PackageManifests`.

Recommended first-version annotations:

- `ElsaFeatureAttribute`: Supplies feature ID, display name, category, description, advanced flag, experimental flag, and extension metadata where supported.
- `FeatureSettingAttribute`: Supplies setting name, display name, description, group, category, required flag, default value metadata, environment variable name, UI hint, secret or sensitive flags, restart-required flag, advanced flag, experimental flag, and extension metadata where supported.
- `ManifestIgnoreAttribute`: Excludes a type or property from manifest generation.
- `ManifestExtensionAttribute`: Supplies small extension metadata values where the contract allows extension data.
- `CompatibilityAttribute`: Supplies package or feature compatibility metadata.
- `RequiresFeatureAttribute`: Declares feature dependencies.
- `ConflictsWithFeatureAttribute`: Declares feature conflicts.

Attribute metadata must be intentionally small. Rich metadata such as long documentation, complex compatibility matrices, icon metadata, and broad extension payloads should live in the override file.

## Override File Model

The default override file name is `elsa-package.overrides.json`. It is optional and may be moved with a project property.

The override file may provide:

- Package display name and description.
- Documentation URLs.
- Icon metadata.
- Tags.
- Compatibility ranges.
- License metadata.
- Package dependencies and conflicts.
- Required runtime capabilities.
- Feature metadata overrides.
- Setting metadata overrides.
- Environment variable mappings.
- UI hints.
- Advanced and experimental flags.
- Extension metadata.

The override file must be validated for structure before merge. Unknown fields are allowed only where the manifest contract or override schema defines extension data. References to unknown features or settings produce diagnostics.

## Merge Behavior

The final manifest is merged in this order:

1. Inferred metadata.
2. XML documentation.
3. Source annotations.
4. Override file.

Conflict rules:

- Scalar values from later sources replace earlier values.
- Null override values clear optional metadata only when the override schema explicitly allows clearing.
- Tags are normalized, de-duplicated case-insensitively, and ordered deterministically.
- Dependencies, conflicts, required capabilities, documentation links, and extension metadata are keyed by stable identifiers where available.
- Feature override entries match by explicit feature ID first, then by CLR type name when feature ID is not supplied.
- Setting override entries match by feature ID plus setting name.
- Duplicate entries in the same source produce diagnostics.
- Override references to nonexistent features or settings produce diagnostics.
- The merge must not silently change package identity or package version to values that conflict with NuGet package identity unless explicitly allowed by future policy.

## JSON Schema Generation

For each feature setting, the generator produces JSON Schema metadata consistent with `Elsa.PackageManifests`.

Type mapping:

- `string` maps to `string`.
- `bool` maps to `boolean`.
- `int`, `long`, `short`, and other integral types map to `integer`.
- `float`, `double`, and `decimal` map to `number`.
- Enums map to `string` with enum values.
- Time duration values map to `string` with duration metadata.
- Date and time values map to `string` with date-time metadata.
- URI values map to `string` with URI metadata.
- Arrays and lists map to `array`.
- Dictionaries map to `object`.
- Safe complex objects map to `object`.
- Nullable values are represented according to the active manifest schema version.
- Unsupported types produce diagnostics according to configured severity.

Validation metadata from common validation annotations should map to JSON Schema constraints when possible, including required, length, range, regular expression, enum values, and custom display metadata where supported.

## Manifest Validation Behavior

The generator validates the final manifest after all inference and overrides are applied.

Validation includes:

- Required manifest fields.
- Supported schema version.
- Package identity and version consistency.
- Feature identity uniqueness.
- Setting identity uniqueness within each feature.
- JSON Schema validity for generated setting metadata.
- Compatibility range syntax.
- Dependency and conflict syntax.
- Extension metadata placement.
- Recommended metadata checks such as missing descriptions, missing documentation links, or missing categories when strict mode is enabled.

Default behavior:

- Schema errors fail the build.
- Unsupported critical setting types fail the build.
- Missing recommended metadata produces warnings.
- Missing XML documentation produces a warning only when descriptions are required by configured policy.
- Strict mode increases recommended metadata checks.
- Fail-on-warnings turns warnings into build failures.

## MSBuild Properties

The generator supports these project properties:

- `GenerateElsaPackageManifest`: Enables or disables generation. Default: `true`.
- `ElsaPackageManifestOutputPath`: Overrides the generated intermediate manifest path.
- `ElsaPackageManifestIncludeInPackage`: Includes the generated manifest in the NuGet package. Default: `true`.
- `ElsaPackageManifestPackagePath`: Overrides the package path. Default: `elsa-package.json`.
- `ElsaPackageManifestOverrideFile`: Overrides the optional override file path. Default: `elsa-package.overrides.json` when present.
- `ElsaPackageManifestValidationSeverity`: Controls validation behavior. Supported values should include `Error`, `Warning`, and `None` for schema validation policy where safe.
- `ElsaPackageManifestStrict`: Enables stricter recommended metadata checks. Default: `false`.
- `ElsaPackageManifestFailOnWarnings`: Treats generator warnings as build failures. Default: `false`.
- `ElsaPackageManifestAllowTargetFrameworkDifferences`: Allows target-specific differences when explicitly accepted. Default: `false`.
- `ElsaPackageManifestDiagnosticsVerbosity`: Controls generator diagnostic verbosity. Default: concise.

## NuGet Package Inclusion Behavior

The canonical NuGet path is the package root: `elsa-package.json`.

Root placement is preferred because:

- It is easy for catalog ingestion to find.
- It avoids coupling the manifest to build assets or content conventions.
- It communicates that the manifest describes the package, not a build-time dependency.

The generator may support alternate package paths through configuration, but the default and recommended path is the root. A package should contain exactly one canonical manifest unless future schema versions explicitly support target-specific manifests.

## Multi-Targeting Behavior

Multi-targeted projects may generate intermediate manifests per target framework for validation, but the package must include a single canonical root manifest by default.

Rules:

- If all target frameworks produce equivalent manifest-relevant surfaces, one canonical manifest is selected and included.
- If target frameworks differ only in target framework metadata, the canonical manifest records the package's supported target frameworks.
- If target frameworks differ in discovered features, settings, or setting schemas, the generator warns or fails according to configured severity.
- Target-specific differences are not silently merged when they change the feature surface.
- Explicit configuration may allow target-specific differences in a future-compatible way, but the MVP should treat differences as suspicious unless deliberately allowed.

## Diagnostics

Diagnostics must be concise, stable, and actionable.

Default diagnostics include:

- Manifest generation enabled or disabled when relevant.
- Generated manifest path.
- Package inclusion path.
- Number of discovered features.
- Missing XML documentation when configured.
- Unsupported feature or setting patterns.
- Unsupported property types.
- Invalid override file structure.
- Override references to missing features or settings.
- Schema validation errors with manifest paths.
- Multi-targeting differences.

Diagnostics should not log every successful property or every inferred value by default. Verbose mode may provide detailed discovery logs for troubleshooting.

## Error Handling

The generator distinguishes between hard errors, validation errors, and recommended metadata warnings.

Hard errors include:

- Missing compiled assembly when generation is enabled and required.
- Unable to inspect assembly metadata.
- Malformed override JSON.
- Inability to load the manifest contract or validation schema.
- Failure to write the output manifest.

Validation errors include:

- Required manifest fields missing.
- Invalid generated schema.
- Duplicate feature IDs.
- Duplicate setting names within a feature.
- Unsupported critical setting types.
- Invalid compatibility ranges.

Warnings include:

- Missing XML documentation.
- Missing recommended descriptions.
- Missing recommended documentation links.
- Ignored unsupported optional metadata.
- Convention-discovered feature ambiguity resolved by explicit metadata.

All diagnostics should include enough context for the package author to locate the type, property, manifest path, or override entry involved.

## Security Considerations

- The generator runs inside the consuming project's build context and may inspect that project's compiled assembly.
- The generator must not execute arbitrary package code.
- The generator must avoid invoking constructors or property getters.
- The generator must use metadata inspection for type, property, annotation, and nullability discovery.
- Override files are local project inputs and must be parsed as data only.
- Generated manifests must not include local secrets or environment-specific values unless explicitly supplied by the package author.
- Secret or sensitive settings must be marked as metadata flags, not populated with secret values.
- Diagnostics must not print secret default values.
- Build output must be deterministic and suitable for CI and reproducible package workflows.

## Testing Strategy

Testing should cover generator behavior from package author and build-system perspectives.

- Unit tests for metadata merge behavior, type mapping, schema generation, XML documentation parsing, override parsing, validation result handling, and deterministic ordering.
- Integration tests with sample package projects that build and pack with the generator reference only.
- Integration tests for direct pack without a separate build command.
- Integration tests for XML documentation present and absent.
- Integration tests for feature discovery through base class, interface, and explicit annotation.
- Integration tests for ignored features and ignored settings.
- Integration tests for nullable reference type metadata.
- Integration tests for common validation annotations.
- Integration tests for unsupported setting types and configured severity behavior.
- Integration tests for malformed and valid override files.
- Integration tests for multi-targeted projects with identical and divergent feature surfaces.
- Package inspection tests that verify exactly one root `elsa-package.json` is included.
- Determinism tests that build the same sample project twice and compare normalized manifest output.
- Safety tests that verify constructors and property getters are not invoked during generation.
- CI tests that run without interactive prompts or machine-specific assumptions.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A package author can add one private package reference and produce a NuGet package containing `elsa-package.json` at the package root without any additional project item configuration.
- **SC-002**: At least 95% of standard package metadata fields are inferred correctly from normal project and NuGet metadata in sample package projects.
- **SC-003**: Feature discovery identifies all intentionally exposed sample CShells features across base-class, interface, and explicit-annotation patterns.
- **SC-004**: Setting discovery identifies all eligible public configurable sample settings and excludes all ignored, static, read-only, and computed-only sample properties.
- **SC-005**: Generated manifests validate against the active `Elsa.PackageManifests` schema in all valid sample projects.
- **SC-006**: Invalid manifests fail the build by default with diagnostics that identify the affected manifest path, feature, setting, or override entry.
- **SC-007**: Building the same sample project twice with unchanged inputs produces byte-stable or semantically equivalent normalized manifest output.
- **SC-008**: Multi-targeted sample projects produce exactly one package-level manifest when feature surfaces match.
- **SC-009**: Divergent multi-targeted feature surfaces produce a warning or error before a package with contradictory metadata is published.
- **SC-010**: Safety tests confirm that feature constructors, property getters, and other package runtime code are not executed during generation.
- **SC-011**: The generator runs successfully in CI using only repository and build inputs.
- **SC-012**: Package authors can resolve common validation issues using build diagnostics without reading generator source code.

## Assumptions

- `Elsa.PackageManifests` exists or will be created as the shared manifest contract package with versioned JSON Schema resources and validation behavior.
- CShells exposes stable base class, interface, or attribute points that can be used for feature identification.
- Package projects can produce XML documentation files when authors want documentation-derived descriptions.
- Package authors are willing to use lightweight annotations or an override file for metadata that cannot be inferred safely.
- The first version optimizes for build-time manifest generation and package inclusion, while analyzer-based authoring assistance can be added later.
- The canonical manifest path for NuGet packages is the package root.
- Target-specific feature differences are uncommon and should be treated conservatively in the first version.

## Acceptance Criteria

1. A library project can reference `Elsa.PackageManifest.Generator` and get an `elsa-package.json` generated automatically.
2. The generated manifest uses the `Elsa.PackageManifests` contract.
3. The manifest is included in the produced NuGet package.
4. Feature classes are discovered from the project assembly.
5. Feature settings are discovered from feature properties.
6. XML documentation comments are used where available.
7. Explicit annotations can override or enrich inferred metadata.
8. An override JSON file can provide additional metadata.
9. The final manifest is validated against the versioned manifest schema.
10. Build diagnostics are clear and actionable.
11. The generator works in CI.
12. The generator supports multi-targeted projects predictably.
13. The generator does not execute package code.

## Open Questions

- What exact CShells base class and interface names should be treated as first-class discovery conventions?
- Which annotation namespace should be owned by the generator package versus the shared manifest contract package?
- Should annotation types live in the generator package, a tiny abstractions package, or `Elsa.PackageManifests` to avoid leaking build-only dependencies into runtime packages?
- Which JSON Schema library and schema draft should `Elsa.PackageManifests` standardize on?
- Should complex object settings be supported in the MVP or limited to primitive, enum, collection, and dictionary shapes first?
- What exact extension metadata shape should be allowed in attributes without becoming a second manifest language?
- Should direct manifest override identity fields ever be allowed to differ from NuGet package identity, or should that always be an error?
- What is the maximum recommended size for generated manifests and override files?

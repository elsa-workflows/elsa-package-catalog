<!--
Sync Impact Report
Version change: template placeholder -> 1.0.0
Modified principles:
- Placeholder principles -> I. Manifest-First Architecture
- Placeholder principles -> II. No Arbitrary Code Execution
- Placeholder principles -> III. Stable Wire Contracts
- Placeholder principles -> IV. Versioned Schema Evolution
- Placeholder principles -> V. Package Versions Are Immutable
- Placeholder principles -> VI. Approval Is Separate From Validity
- Placeholder principles -> VII. Explicit Sources Only
- Placeholder principles -> VIII. Public API Safe By Default
- Placeholder principles -> IX. Operational Debuggability Over Cleverness
- Placeholder principles -> X. Modular Monolith First
- Placeholder principles -> XI. Runtime Builder Readiness
- Placeholder principles -> XII. Separation Of Concerns
- Placeholder principles -> XIII. Security-Conscious Ecosystem Design
- Placeholder principles -> XIV. Compatibility Must Be Explicit
- Placeholder principles -> XV. Keep The First Version Small
- Placeholder principles -> XVI. Simplicity First
- Placeholder principles -> XVII. Modern C# Idioms
- Placeholder principles -> XVIII. Readability Over Cleverness
- Placeholder principles -> XIX. Explicitness Over Magic
- Placeholder principles -> XX. Abstractions Must Earn Their Existence
- Placeholder principles -> XXI. Optimize For Deletion
- Placeholder principles -> XXII. Favor Composition Over Inheritance
- Placeholder principles -> XXIII. Dependencies Should Remain Intentional
- Placeholder principles -> XXIV. Operational Simplicity Matters
Added sections:
- Architectural And Product Constraints
- Development Workflow And Quality Gates
Removed sections:
- Template placeholder sections
Templates requiring updates:
- updated: .specify/templates/plan-template.md
- updated: .specify/templates/tasks-template.md
- no change required: .specify/templates/spec-template.md
Follow-up TODOs: none
-->

# Elsa Package Catalog Constitution

## Core Principles

### I. Manifest-First Architecture

Package metadata MUST be exchanged through explicit, versioned manifests. The
catalog MUST consume manifests and MUST NOT infer package behavior by executing
package code.

Rationale: manifests provide a deterministic, reviewable contract between package
publishers, catalog indexing, runtime validation, and future builder tooling.

### II. No Arbitrary Code Execution

The catalog MUST NOT load or execute assemblies from indexed packages. It MAY
inspect package files, nuspec metadata, package archive structure, and manifest
JSON only.

Rationale: indexed packages are untrusted inputs. Safe inspection preserves trust
in the catalog and prevents supply-chain execution risks.

### III. Stable Wire Contracts

`Elsa.PackageManifests` defines the public manifest wire contract. It MUST remain
dependency-light, versioned, forward-compatible where possible, and separate from
catalog persistence models, service internals, and runtime implementation details.

Rationale: the manifest package is shared by generator, catalog, runtime
validation, and builder tooling, so contract drift would harm the ecosystem.

### IV. Versioned Schema Evolution

Manifest schemas MUST be versioned independently from NuGet package versions.
Unknown extension metadata SHOULD be preserved where reasonable. Breaking schema
changes MUST require an explicit new schema version.

Rationale: package versions and manifest schema versions change for different
reasons. Independent versioning allows safe evolution without rewriting package
history.

### V. Package Versions Are Immutable

A package ID and package version MUST be treated as immutable. If an already
indexed version later produces different manifest content, the catalog MUST flag
the record as suspicious and MUST NOT silently replace the stored manifest.

Rationale: immutable version handling protects downstream runtime composition from
unexpected package behavior and enables auditability.

### VI. Approval Is Separate From Validity

A valid manifest MUST NOT imply public listing. Package approval, package-version
approval, listing state, and validation status MUST remain separate concerns.

Rationale: technical correctness is not the same as ecosystem trust, commercial
approval, or public discoverability.

### VII. Explicit Sources Only

The catalog MUST scan configured package sources using include and exclude
patterns. It MUST NOT broadly crawl package ecosystems or infer discovery scope
from global feeds.

Rationale: explicit source configuration keeps indexing intentional, debuggable,
and bounded.

### VIII. Public API Safe By Default

Public APIs MUST expose only package versions that are valid, approved, and
listed. Admin APIs MAY expose invalid, rejected, suspicious, pending, or unlisted
records when authenticated and authorized.

Rationale: public consumers should receive a curated catalog surface by default,
while administrators still need full diagnostic visibility.

### IX. Operational Debuggability Over Cleverness

Sync runs, validation errors, indexing decisions, approval decisions, and
suspicious changes MUST be persisted and inspectable. Failed package indexing
MUST NOT destroy the whole sync run.

Rationale: catalog correctness depends on being able to explain why packages were
indexed, skipped, rejected, hidden, or marked suspicious.

### X. Modular Monolith First

The service MUST start as a clean ASP.NET Core modular monolith. Distributed
infrastructure MUST be avoided unless clearly justified by product requirements.
SQLite is acceptable initially, and the domain model MUST NOT prevent later
PostgreSQL adoption.

Rationale: the first version needs clear boundaries and low operational cost more
than distributed scalability.

### XI. Runtime Builder Readiness

APIs and manifests SHOULD support future UI-driven runtime configuration,
including package discovery, feature selection, settings schemas, compatibility
checks, and later deployment bundle generation.

Rationale: the catalog is a foundation for professional Elsa runtime composition,
even when the builder UI itself is not in scope.

### XII. Separation Of Concerns

Package installation, feature availability, feature enablement, feature
configuration, package approval, licensing, and compatibility MUST remain
separate concepts.

Rationale: mixing these concerns makes approval, validation, runtime selection,
and future UI behavior harder to reason about.

### XIII. Security-Conscious Ecosystem Design

Third-party packages MUST be treated as untrusted until approved. Catalog
processing MUST be deterministic, auditable, and safe.

Rationale: the catalog sits in a professional runtime supply chain and must
prioritize trust boundaries from the start.

### XIV. Compatibility Must Be Explicit

Compatibility with Elsa versions, Docker image versions, package versions,
runtime capabilities, package dependencies, feature dependencies, and feature
conflicts SHOULD be represented explicitly and MUST NOT be guessed from package
implementation details.

Rationale: explicit compatibility metadata allows deterministic validation and
actionable feedback to builder tooling.

### XV. Keep The First Version Small

The first version SHOULD prefer a simple, correct, inspectable catalog over a
broad platform. Runtime Builder UI, manifest generation, Sigil licensing, full
dependency resolution, and Docker deployment bundle generation MUST be deferred
unless explicitly scoped.

Rationale: a small first version lowers risk and creates a reliable foundation for
later ecosystem capabilities.

### XVI. Simplicity First

Solutions SHOULD use the simplest architecture and implementation that correctly
satisfy current requirements. Speculative abstractions, premature generalization,
and unnecessary infrastructure SHOULD be avoided.

Rationale: simple systems are easier to verify, operate, and change.

### XVII. Modern C# Idioms

Code SHOULD leverage modern C# and .NET features when they improve clarity,
correctness, and conciseness. Examples include records, primary constructors,
collection expressions, pattern matching, async streams, minimal APIs where
appropriate, and nullable reference types.

Rationale: modern idioms can reduce boilerplate and make intent clearer when used
judiciously.

### XVIII. Readability Over Cleverness

Code MUST optimize for maintainability and clarity by experienced .NET
developers. Cleverness, excessive indirection, unnecessary metaprogramming, and
surprising control flow SHOULD be avoided.

Rationale: catalog behavior must remain easy to inspect because it controls
trusted package discovery.

### XIX. Explicitness Over Magic

Behavior SHOULD be discoverable through code and configuration rather than hidden
conventions, runtime scanning, or implicit side effects.

Rationale: explicit behavior is easier to audit, test, document, and debug.

### XX. Abstractions Must Earn Their Existence

Interfaces, layers, generic pipelines, base classes, and extension points SHOULD
only be introduced when they solve a demonstrated current need.

Rationale: unnecessary abstraction creates framework work inside the product and
makes the first version harder to complete.

### XXI. Optimize For Deletion

The architecture SHOULD make components easy to remove, replace, or simplify.
Small composable modules SHOULD be preferred over deeply intertwined systems.

Rationale: deletion-friendly design keeps future migrations and scope reductions
practical.

### XXII. Favor Composition Over Inheritance

Composition, explicit contracts, and data-oriented design SHOULD generally be
preferred over deep inheritance hierarchies.

Rationale: composition keeps behavior local and reduces coupling between catalog
concepts.

### XXIII. Dependencies Should Remain Intentional

The solution SHOULD minimize unnecessary third-party dependencies. Platform
capabilities SHOULD be preferred before introducing additional frameworks.

Rationale: every dependency adds maintenance, security, and operational surface
area.

### XXIV. Operational Simplicity Matters

Deployment, debugging, local development, and observability SHOULD remain
straightforward. Operational complexity MUST be justified by clear product value.

Rationale: professional runtime infrastructure must be understandable and
supportable by a small team.

## Architectural And Product Constraints

- The repository is part of the Elsa professional runtime ecosystem and MUST keep
  catalog, manifest contract, validation, and future builder concerns aligned.
- The initial product shape MUST include an ASP.NET Core Catalog API and the
  shared `Elsa.PackageManifests` contract package.
- The catalog MUST inspect only configured package sources and approved package
  artifacts.
- Manifest files, validation results, package-version records, approval state,
  and sync history MUST be durable and auditable.
- Public API behavior MUST prefer hiding unsafe or unapproved records over
  exposing uncertain data.
- Admin API behavior MUST provide enough detail to explain validation, approval,
  listing, sync, and suspicious-change decisions.

## Development Workflow And Quality Gates

- Every feature plan MUST include a Constitution Check that addresses manifest
  contracts, arbitrary code execution, approval separation, public API safety,
  sync debuggability, simplicity, and operational impact.
- New manifest fields or schema changes MUST document schema version impact and
  forward-compatibility behavior.
- Any code that processes NuGet packages MUST include tests or verification that
  package assemblies are not loaded or executed.
- Public API changes MUST include tests or verification that invalid, unapproved,
  rejected, suspicious, and unlisted package versions are hidden.
- Sync behavior changes MUST include persisted diagnostics for failures,
  validation errors, indexing decisions, and suspicious manifest changes.
- New abstractions, layers, or third-party dependencies MUST be justified by a
  demonstrated need in the implementation plan.
- Implementation tasks SHOULD remain organized by independently testable user
  stories and SHOULD include observability, security, and cleanup tasks where
  relevant.

## Governance

This constitution supersedes conflicting repository guidance for architecture,
product scope, and implementation quality. Feature specifications, plans, tasks,
and code reviews MUST check compliance with these principles.

Amendments MUST be made by updating this constitution with a Sync Impact Report,
including affected principles, dependent template changes, and follow-up work.
Version changes follow semantic versioning:

- MAJOR version changes redefine or remove existing governance rules.
- MINOR version changes add principles or materially expand existing rules.
- PATCH version changes clarify wording without changing meaning.

Reviewers MUST reject changes that violate MUST-level rules unless the
constitution is amended first. SHOULD-level rules may be bypassed only when the
implementation plan documents a clear product reason and a simpler alternative
that was considered.

**Version**: 1.0.0 | **Ratified**: 2026-05-14 | **Last Amended**: 2026-05-14

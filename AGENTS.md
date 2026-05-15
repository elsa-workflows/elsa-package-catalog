<!-- SPECKIT START -->
For additional context about technologies to be used, project structure,
shell commands, and other important information, read the current plan:
specs/003-generator-adoption-fixes/plan.md
<!-- SPECKIT END -->

## Active Technologies
- C# on .NET 10 LTS + ASP.NET Core, Entity Framework Core, SQLite, NuGet.Protocol, System.Text.Json, JSON Schema validation, xUnit, FluentAssertions (001-package-catalog)
- SQLite for initial durable storage with provider-neutral EF Core/domain design for later PostgreSQL support (001-package-catalog)
- C# on .NET 10 LTS + MSBuild task APIs, System.Reflection.Metadata, MetadataLoadContext, System.Xml.Linq, System.Text.Json, JsonSchema.Net, Elsa.PackageManifests, NuGet.Versioning, xUnit, FluentAssertions (002-package-manifest-generator)
- File artifact generation only: compiled assemblies, XML docs, project/NuGet metadata, reference metadata, optional overrides in; deterministic `elsa-package.json` and NuGet package root inclusion out (002-package-manifest-generator)
- C# on .NET 10 LTS with nullable reference types + existing MSBuild task APIs, metadata inspection, System.Text.Json, Elsa.PackageManifests validation, xUnit, FluentAssertions (003-generator-adoption-fixes)
- File artifact generation only: compiled assemblies/XML docs/project metadata in; deterministic `elsa-package.json` and NuGet package root inclusion out (003-generator-adoption-fixes)

## Recent Changes
- 001-package-catalog: Added implementation plan, research, data model, OpenAPI contract, quickstart, and current plan reference.
- 001-package-catalog: Shifted project structure to onion-style `Elsa.Catalog.Core`, `Elsa.Catalog.Persistence.EntityFrameworkCore`, and `Elsa.Catalog.Packaging.NuGet`.
- 002-package-manifest-generator: Added implementation plan, research, data model, MSBuild/annotation/package-layout contracts, override schema, and quickstart.
- 003-generator-adoption-fixes: Added plan, research, data model, diagnostic policy, setting discovery, package inclusion contracts, and quickstart.

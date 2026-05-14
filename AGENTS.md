<!-- SPECKIT START -->
For additional context about technologies to be used, project structure,
shell commands, and other important information, read the current plan
<!-- SPECKIT END -->

## Active Technologies
- C# on .NET 10 LTS + ASP.NET Core, Entity Framework Core, SQLite, NuGet.Protocol, System.Text.Json, JSON Schema validation, xUnit, FluentAssertions (001-package-catalog)
- SQLite for initial durable storage with provider-neutral EF Core/domain design for later PostgreSQL support (001-package-catalog)

## Recent Changes
- 001-package-catalog: Added implementation plan, research, data model, OpenAPI contract, quickstart, and current plan reference.
- 001-package-catalog: Shifted project structure to onion-style `Elsa.Catalog.Core`, `Elsa.Catalog.Persistence.EntityFrameworkCore`, and `Elsa.Catalog.Packaging.NuGet`.

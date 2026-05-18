# Quickstart: Server-Side Planning

## Scenario 1: Plan Adds Dependencies

Submit intent with one feature requiring another package.

Expected: response includes auto-added package or blocking finding.

## Scenario 2: Infrastructure Autofill

Submit intent selecting PostgreSQL persistence without infrastructure.

Expected: response selects default PostgreSQL provider when unambiguous.

## Scenario 3: Bundle Uses Plan

Generate bundle for same intent.

Expected: bundle findings and infrastructure match planner output.

## Validation Commands

```bash
dotnet build Elsa.PackageCatalog.sln --no-restore
dotnet test Elsa.PackageCatalog.sln --no-build
```

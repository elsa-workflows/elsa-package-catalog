# Quickstart: Deployment Template Expansion

## Scenario 1: Default Target

Omit `target`.

Expected: Docker Compose files are returned.

## Scenario 2: Azure Container Apps

Set `target` to `azure-container-apps`.

Expected: Azure template files and README are returned.

## Scenario 3: Kubernetes/Helm

Set `target` to `kubernetes-helm`.

Expected: Helm/Kubernetes files and README are returned.

## Validation Commands

```bash
dotnet build Elsa.PackageCatalog.sln --no-restore
dotnet test Elsa.PackageCatalog.sln --no-build
```

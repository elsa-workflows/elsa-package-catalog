# Aspire Deployment to Azure App Service

## Recommendation

Use Aspire and Azure Developer CLI (`azd`) as the deployment path. The AppHost
declares the API, Azure App Service environment, and Azure SQL database;
Aspire/azd provisions the resources, builds the app container image, pushes it
to ACR, and deploys the Web App.

The previous manually provisioned Web App can be deleted once anything important
has been backed up.

## Local Tooling

Install the current .NET 10 SDK before deploying. As of May 15, 2026, the
current .NET 10 SDK line is `10.0.300`, released May 12, 2026.

The official Aspire templates are installed with:

```bash
dotnet new install Aspire.ProjectTemplates
dotnet tool install -g Aspire.Cli
```

On this machine, `~/.dotnet/tools/aspire` is `13.3.2`. If `aspire --version`
prints an older version, move `~/.dotnet/tools` before `~/.aspire/bin` in
`PATH`, or run `~/.dotnet/tools/aspire` explicitly.

## Deploy

```bash
azd auth login
azd init
azd env set adminApiKey <strong-secret>
azd up
```

When `azd init` asks how to initialize the app, scan the current directory and
confirm the detected Aspire AppHost.

## GitHub Actions Deployment

The `Azure API Deploy` workflow runs on pushes to `main` and can also be run
manually from GitHub Actions. Normal `main` deployments run:

```bash
azd deploy --no-prompt
```

This is the fast path for application-only updates because the Azure resources
are expected to already exist for the selected `azd` environment. If the AppHost
infrastructure shape changes, run the same workflow manually and choose
`deploy_mode: infra`; that path runs:

```bash
azd up --no-prompt
```

`azd up` provisions infrastructure incrementally before deploying. Keep it as a
manual choice so routine code changes do not spend time checking and updating
Azure resources on every push.

Configure the workflow in a GitHub environment named `production` unless you
change the workflow environment name. With OIDC, the Microsoft Entra federated
credential should trust this repository and environment. If using the default
GitHub environment subject, it is:

```text
repo:<owner>/<repo>:environment:production
```

Required GitHub Actions variables:

- `AZURE_CLIENT_ID`: application/client ID for the federated identity.
- `AZURE_TENANT_ID`: Microsoft Entra tenant ID.
- `AZURE_SUBSCRIPTION_ID`: target Azure subscription ID.
- `AZURE_ENV_NAME`: existing or desired `azd` environment name, for example
  `elsa-package-catalog`.
- `AZURE_LOCATION`: Azure region for the `azd` environment, for example
  `westeurope`.

Required GitHub Actions secrets:

- `ADMIN_API_KEY`: strong API key passed to the AppHost `adminApiKey` parameter
  and surfaced to the API as `Authentication__ApiKey`.

The workflow validates the configuration, restores the solution, builds the
Aspire AppHost, runs the API test project, signs in with `azd auth login` using
GitHub federated credentials, creates the local CI `azd` environment metadata,
sets the secured `infra.parameters.adminApiKey` parameter for the run, and
deploys.

## Removing Existing Resources

If the old resources are in a dedicated resource group, delete the group:

```bash
az group delete --name <old-resource-group>
```

If the group has shared resources, delete only the old Web App, plan, registry,
storage account, and related managed identities after confirming they are not
used elsewhere.

## Database Provider

The API supports two EF Core providers:

- `Database:Provider=Sqlite`
- `Database:Provider=SqlServer`

Local development defaults to SQLite. Aspire publish mode provisions Azure SQL,
injects `ConnectionStrings__Catalog`, and sets `Database__Provider=SqlServer`.

Each provider has its own EF Core migration assembly:

- SQLite: `Elsa.Catalog.Persistence.SqliteMigrations`
- SQL Server/Azure SQL: `Elsa.Catalog.Persistence.SqlServerMigrations`

The API selects the matching migration assembly with the provider and applies
migrations at startup outside the `Testing` environment.

SQLite remains fine for local development and single-process test runs. For
production and App Service scale-out, use Azure SQL. SQLite on shared App
Service storage or Azure Files is not a good production target because SQLite
depends on filesystem locking, and WAL mode does not support clients on
different machines through a network filesystem.

var builder = DistributedApplication.CreateBuilder(args);
var adminApiKey = builder.AddParameter("adminApiKey", secret: true);

builder.AddAzureAppServiceEnvironment("elsa-package-catalog");

var api = builder.AddProject<Projects.Elsa_Catalog_Api>("api")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithEnvironment("Authentication__ApiKey", adminApiKey);

if (builder.ExecutionContext.IsPublishMode)
{
    var sql = builder.AddAzureSqlServer("catalog-sql");
    var database = sql.AddDatabase("Catalog");

    api.WithReference(database)
        .WithEnvironment("Database__Provider", "SqlServer");
}
else
{
    api.WithEnvironment("Database__Provider", "Sqlite")
        .WithEnvironment("ConnectionStrings__Catalog", "Data Source=elsa-catalog-dev.db");
}

builder.Build().Run();

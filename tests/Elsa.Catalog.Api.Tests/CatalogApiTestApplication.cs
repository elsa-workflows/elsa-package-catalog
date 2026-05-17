using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Catalog.Api.Authentication;
using Elsa.Catalog.Persistence.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Catalog.Api.Tests;

internal sealed class CatalogApiTestApplication : WebApplicationFactory<Program>, IAsyncDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"elsa-catalog-{Guid.NewGuid():N}.db");

    public static JsonSerializerOptions JsonOptions { get; } = CreateJsonOptions();

    public string ConnectionString => $"Data Source={_databasePath}";

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ApiKeyAuthenticationDefaults.ConfigurationKey] = "local-dev-key",
                [TrustedHeaderWorkspaceIdentityReader.EnabledConfigurationKey] = "true"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<CatalogDbContext>>();

            services.AddDbContext<CatalogDbContext>(options =>
                options.UseSqlite(ConnectionString, sqlite =>
                {
                    sqlite.MigrationsAssembly(CatalogDatabaseServiceCollectionExtensions.SqliteMigrationsAssembly);
                }));
        });
    }

    public async Task SeedAsync(Func<CatalogDbContext, Task> seed)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
        await seed(db);
        await db.SaveChangesAsync();
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await base.DisposeAsync();

        if (File.Exists(_databasePath))
            File.Delete(_databasePath);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
        return options;
    }
}

internal static class CatalogApiJsonExtensions
{
    public static Task<T?> GetCatalogJsonAsync<T>(this HttpClient client, string requestUri, CancellationToken cancellationToken = default) =>
        client.GetFromJsonAsync<T>(requestUri, CatalogApiTestApplication.JsonOptions, cancellationToken);

    public static Task<HttpResponseMessage> PostCatalogJsonAsync<T>(this HttpClient client, string requestUri, T value, CancellationToken cancellationToken = default) =>
        client.PostAsJsonAsync(requestUri, value, CatalogApiTestApplication.JsonOptions, cancellationToken);

    public static Task<HttpResponseMessage> PutCatalogJsonAsync<T>(this HttpClient client, string requestUri, T value, CancellationToken cancellationToken = default) =>
        client.PutAsJsonAsync(requestUri, value, CatalogApiTestApplication.JsonOptions, cancellationToken);

    public static Task<T?> ReadCatalogJsonAsync<T>(this HttpContent content, CancellationToken cancellationToken = default) =>
        content.ReadFromJsonAsync<T>(CatalogApiTestApplication.JsonOptions, cancellationToken);
}

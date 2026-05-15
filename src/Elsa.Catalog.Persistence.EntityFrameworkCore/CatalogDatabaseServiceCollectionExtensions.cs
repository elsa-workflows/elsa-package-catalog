using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Catalog.Persistence.EntityFrameworkCore;

public static class CatalogDatabaseServiceCollectionExtensions
{
    public const string DefaultSqliteConnectionString = "Data Source=elsa-catalog.db";
    public const string SqliteMigrationsAssembly = "Elsa.Catalog.Persistence.SqliteMigrations";
    public const string SqlServerMigrationsAssembly = "Elsa.Catalog.Persistence.SqlServerMigrations";

    public static IServiceCollection AddCatalogDbContext(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var databaseOptions = configuration
            .GetSection(CatalogDatabaseOptions.SectionName)
            .Get<CatalogDatabaseOptions>() ?? new CatalogDatabaseOptions();

        var connectionString = configuration.GetConnectionString("Catalog");

        services.AddDbContext<CatalogDbContext>(options =>
            ConfigureProvider(options, databaseOptions.Provider, connectionString));

        return services;
    }

    private static void ConfigureProvider(
        DbContextOptionsBuilder options,
        CatalogDatabaseProvider provider,
        string? connectionString)
    {
        switch (provider)
        {
            case CatalogDatabaseProvider.Sqlite:
                var sqliteConnectionString = string.IsNullOrWhiteSpace(connectionString)
                    ? DefaultSqliteConnectionString
                    : connectionString;

                EnsureSqliteDirectoryExists(sqliteConnectionString);
                options.UseSqlite(sqliteConnectionString, sqlite =>
                {
                    sqlite.MigrationsAssembly(SqliteMigrationsAssembly);
                });
                break;

            case CatalogDatabaseProvider.SqlServer:
                if (string.IsNullOrWhiteSpace(connectionString))
                    throw new InvalidOperationException("ConnectionStrings:Catalog is required when Database:Provider is SqlServer.");

                options.UseSqlServer(connectionString, sqlServer =>
                {
                    sqlServer.MigrationsAssembly(SqlServerMigrationsAssembly);
                    sqlServer.EnableRetryOnFailure();
                });
                break;

            default:
                throw new InvalidOperationException($"Unsupported catalog database provider '{provider}'.");
        }
    }

    private static void EnsureSqliteDirectoryExists(string connectionString)
    {
        var dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;
        if (string.IsNullOrWhiteSpace(dataSource) || dataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase))
            return;

        var directory = Path.GetDirectoryName(Path.GetFullPath(dataSource));
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);
    }
}

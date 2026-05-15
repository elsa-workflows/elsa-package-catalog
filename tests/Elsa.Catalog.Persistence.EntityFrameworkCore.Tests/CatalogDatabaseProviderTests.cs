using Elsa.Catalog.Persistence.EntityFrameworkCore;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Catalog.Persistence.EntityFrameworkCore.Tests;

public sealed class CatalogDatabaseProviderTests
{
    [Fact]
    public void AddCatalogDbContext_defaults_to_sqlite()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>());
        using var scope = provider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        db.Database.ProviderName.Should().Be("Microsoft.EntityFrameworkCore.Sqlite");
    }

    [Fact]
    public void AddCatalogDbContext_uses_sql_server_when_configured()
    {
        var settings = new Dictionary<string, string?>
        {
            ["Database:Provider"] = "SqlServer",
            ["ConnectionStrings:Catalog"] = "Server=tcp:catalog.database.windows.net,1433;Initial Catalog=Catalog;Authentication=Active Directory Default;"
        };

        using var provider = BuildProvider(settings);
        using var scope = provider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        db.Database.ProviderName.Should().Be("Microsoft.EntityFrameworkCore.SqlServer");
    }

    [Fact]
    public void AddCatalogDbContext_requires_sql_server_connection_string()
    {
        var settings = new Dictionary<string, string?>
        {
            ["Database:Provider"] = "SqlServer"
        };

        using var provider = BuildProvider(settings);
        using var scope = provider.CreateScope();

        var act = () => scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ConnectionStrings:Catalog*SqlServer*");
    }

    private static ServiceProvider BuildProvider(IReadOnlyDictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddCatalogDbContext(configuration);

        return services.BuildServiceProvider();
    }
}

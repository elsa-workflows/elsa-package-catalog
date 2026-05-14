using Elsa.Catalog.Core.Packages;
using Elsa.Catalog.Persistence.EntityFrameworkCore;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Catalog.Persistence.EntityFrameworkCore.Tests;

public sealed class CatalogDbContextMappingTests
{
    [Fact]
    public async Task Can_create_schema_and_store_package_source()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        await using var db = new CatalogDbContext(options);
        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();

        db.PackageSources.Add(new PackageSource
        {
            Name = "NuGet",
            Url = "https://api.nuget.org/v3/index.json",
            IncludePatterns = ["Elsa.*"]
        });
        await db.SaveChangesAsync();

        (await db.PackageSources.CountAsync()).Should().Be(1);
    }
}

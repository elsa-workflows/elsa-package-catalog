namespace Elsa.Catalog.Persistence.EntityFrameworkCore;

public sealed class CatalogDatabaseOptions
{
    public const string SectionName = "Database";

    public CatalogDatabaseProvider Provider { get; set; } = CatalogDatabaseProvider.Sqlite;
}

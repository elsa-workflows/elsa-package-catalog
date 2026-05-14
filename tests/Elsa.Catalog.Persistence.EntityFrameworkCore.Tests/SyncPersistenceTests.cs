using Elsa.Catalog.Core.Packages;
using Elsa.Catalog.Core.Sync;
using Elsa.Catalog.Persistence.EntityFrameworkCore;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Catalog.Persistence.EntityFrameworkCore.Tests;

public sealed class SyncPersistenceTests
{
    [Fact]
    public async Task Persists_sync_run_items_for_diagnostics()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        await using var db = new CatalogDbContext(options);
        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();

        var run = new SyncRun { Trigger = SyncRunTrigger.ManualAll };
        run.Items.Add(new SyncRunItem { SyncRun = run, SyncRunId = run.Id, PackageId = "Elsa.Email", Version = "1.0.0", Status = SyncRunItemStatus.Failed, Error = "No manifest" });
        db.SyncRuns.Add(run);
        await db.SaveChangesAsync();

        var stored = await db.SyncRuns.Include(x => x.Items).SingleAsync();

        stored.Items.Should().ContainSingle(x => x.Error == "No manifest");
    }
}

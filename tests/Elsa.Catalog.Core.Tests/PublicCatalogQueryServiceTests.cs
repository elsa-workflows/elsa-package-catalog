using Elsa.Catalog.Core.Packages;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;

namespace Elsa.Catalog.Core.Tests;

public sealed class PublicCatalogQueryServiceTests
{
    [Fact]
    public async Task Delegates_public_catalog_reads_to_query_port()
    {
        var queries = new CapturingPublicCatalogQueries();
        var service = new PublicCatalogQueryService(queries, CreateCache());

        var packages = await service.ListPackagesAsync();

        packages.Should().ContainSingle(x => x.PackageId == "Elsa.Email");
        queries.ListPackagesCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Caches_public_catalog_reads_until_invalidated()
    {
        var queries = new CapturingPublicCatalogQueries();
        var cache = CreateCache();
        var service = new PublicCatalogQueryService(queries, cache);

        await service.ListPackagesAsync();
        await service.ListPackagesAsync();
        cache.Invalidate();
        await service.ListPackagesAsync();

        queries.ListPackagesCallCount.Should().Be(2);
    }

    [Fact]
    public async Task Serializes_concurrent_cache_misses_for_the_same_key()
    {
        var queries = new CapturingPublicCatalogQueries();
        var service = new PublicCatalogQueryService(queries, CreateCache());

        await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => service.ListPackagesAsync()));

        queries.ListPackagesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Canceled_waiter_does_not_release_or_remove_unacquired_key_lock()
    {
        var cache = CreateCache();
        var factoryCalls = 0;
        var factoryStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFactory = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = cache.GetOrCreateAsync("packages:list", async _ =>
        {
            Interlocked.Increment(ref factoryCalls);
            factoryStarted.SetResult();
            await releaseFactory.Task;
            return 1;
        });
        await factoryStarted.Task;

        using var canceledWait = new CancellationTokenSource();
        var waiting = cache.GetOrCreateAsync("packages:list", _ =>
        {
            Interlocked.Increment(ref factoryCalls);
            return Task.FromResult(2);
        }, canceledWait.Token);
        canceledWait.Cancel();

        var canceledAct = async () => await waiting;
        await canceledAct.Should().ThrowAsync<OperationCanceledException>();

        var next = cache.GetOrCreateAsync("packages:list", _ =>
        {
            Interlocked.Increment(ref factoryCalls);
            return Task.FromResult(3);
        });
        factoryCalls.Should().Be(1);

        releaseFactory.SetResult();
        (await first).Should().Be(1);
        (await next).Should().Be(1);
        factoryCalls.Should().Be(1);
    }

    private static PublicCatalogCache CreateCache() =>
        new(new MemoryCache(new MemoryCacheOptions()));

    private sealed class CapturingPublicCatalogQueries : IPublicCatalogQueries
    {
        public bool ListPackagesCalled { get; private set; }
        public int ListPackagesCallCount { get; private set; }

        public Task<IReadOnlyList<PublicPackageProjection>> ListPackagesAsync(CancellationToken cancellationToken = default)
        {
            ListPackagesCalled = true;
            ListPackagesCallCount++;
            return Task.FromResult<IReadOnlyList<PublicPackageProjection>>([new PublicPackageProjection("Elsa.Email", new PublicPackageSourceProjection(Guid.NewGuid(), "Test NuGet", "https://example.test/v3/index.json"), "1.0.0", [])]);
        }

        public Task<PublicPackageProjection?> GetPackageAsync(string packageId, CancellationToken cancellationToken = default) => Task.FromResult<PublicPackageProjection?>(null);
        public Task<IReadOnlyList<PublicPackageVersionProjection>> ListVersionsAsync(string packageId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PublicPackageVersionProjection>>([]);
        public Task<PublicPackageVersionProjection?> GetVersionAsync(string packageId, string version, CancellationToken cancellationToken = default) => Task.FromResult<PublicPackageVersionProjection?>(null);
        public Task<IReadOnlyList<PublicFeatureProjection>> ListFeaturesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PublicFeatureProjection>>([]);
        public Task<PublicFeatureProjection?> GetFeatureAsync(string featureId, CancellationToken cancellationToken = default) => Task.FromResult<PublicFeatureProjection?>(null);
    }
}

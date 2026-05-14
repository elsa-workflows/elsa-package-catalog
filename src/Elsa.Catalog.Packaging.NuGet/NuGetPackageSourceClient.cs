namespace Elsa.Catalog.Packaging.NuGet;

public sealed class NuGetPackageSourceClient
{
    public Task<IReadOnlyList<string>> FindPackageVersionsAsync(string sourceUrl, string packageId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);
        return Task.FromResult<IReadOnlyList<string>>([]);
    }
}

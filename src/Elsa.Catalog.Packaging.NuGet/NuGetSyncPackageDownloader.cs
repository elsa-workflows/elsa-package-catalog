namespace Elsa.Catalog.Packaging.NuGet;

public sealed class NuGetSyncPackageDownloader
{
    public Task<Stream> DownloadPackageAsync(string sourceUrl, string packageId, string version, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        throw new NotImplementedException("NuGet package downloads are implemented in the sync story.");
    }
}

using System.IO.Compression;
using System.Security.Cryptography;

namespace Elsa.Catalog.Packaging.NuGet;

public sealed class PackageArchiveManifestReader
{
    public const string RootManifestPath = "elsa-package.json";
    public const string FallbackManifestPath = "build/elsa-package.json";

    public async Task<PackageManifestReadResult> ReadAsync(Stream packageStream, CancellationToken cancellationToken = default)
    {
        using var archive = new ZipArchive(packageStream, ZipArchiveMode.Read, leaveOpen: true);
        var entries = archive.Entries
            .Where(entry => string.Equals(Normalize(entry.FullName), RootManifestPath, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(Normalize(entry.FullName), FallbackManifestPath, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var selected = entries.FirstOrDefault(entry => string.Equals(Normalize(entry.FullName), RootManifestPath, StringComparison.OrdinalIgnoreCase))
                       ?? entries.FirstOrDefault(entry => string.Equals(Normalize(entry.FullName), FallbackManifestPath, StringComparison.OrdinalIgnoreCase));

        if (selected is null)
            return PackageManifestReadResult.Missing();

        await using var manifestStream = selected.Open();
        using var memory = new MemoryStream();
        await manifestStream.CopyToAsync(memory, cancellationToken);
        var bytes = memory.ToArray();
        var json = System.Text.Encoding.UTF8.GetString(bytes);
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var warnings = entries.Count > 1
            ? new[] { "Multiple manifest files found; selected the root manifest when available." }
            : [];

        return PackageManifestReadResult.Found(Normalize(selected.FullName), json, hash, warnings);
    }

    private static string Normalize(string path) => path.Replace('\\', '/').TrimStart('/');
}

public sealed record PackageManifestReadResult(
    bool Exists,
    string? Path,
    string? ManifestJson,
    string? ManifestHash,
    IReadOnlyList<string> Warnings)
{
    public static PackageManifestReadResult Missing() => new(false, null, null, null, []);

    public static PackageManifestReadResult Found(string path, string manifestJson, string manifestHash, IReadOnlyList<string> warnings) =>
        new(true, path, manifestJson, manifestHash, warnings);
}

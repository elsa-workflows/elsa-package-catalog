using Elsa.Catalog.Core.Packages;
using Elsa.Catalog.Core.Sources;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NuGet.Versioning;

namespace Elsa.Catalog.Packaging.NuGet.Tests;

public sealed class NuGetPackageSourceClientTests
{
    [Fact]
    public async Task Wildcard_only_sources_do_not_trigger_broad_feed_crawling()
    {
        var client = new NuGetPackageSourceClient(new PackageSourcePatternMatcher());
        var source = new PackageSource
        {
            Url = "https://example.invalid/v3/index.json",
            IncludePatterns = ["Elsa.*"]
        };

        var act = () => client.FindPackageVersionsAsync(source);

        await act.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*exact package ID include pattern*");
    }

    [Fact]
    public void Latest_stable_policy_selects_highest_non_prerelease_version()
    {
        var selected = Select(
            PackageSourceVersionDiscoveryPolicy.LatestStable,
            "3.0.2",
            "3.7.0-preview.4512",
            "2.9.0");

        selected.Should().ContainSingle().Which.Should().Be("3.0.2");
    }

    [Fact]
    public void Latest_stable_policy_logs_when_all_versions_are_prerelease()
    {
        var logger = new ListLogger<NuGetPackageSourceClient>();
        var selected = NuGetPackageSourceClient
            .SelectVersionsForPackage(
                new PackageSource
                {
                    Name = "NuGet",
                    VersionDiscoveryPolicy = PackageSourceVersionDiscoveryPolicy.LatestStable
                },
                "Elsa",
                [NuGetVersion.Parse("3.7.0-preview.4511"), NuGetVersion.Parse("3.7.0-preview.4512")],
                logger);

        selected.Should().BeEmpty();
        logger.Messages.Should().ContainSingle(message =>
            message.Level == LogLevel.Warning &&
            message.Text.Contains("only prerelease versions", StringComparison.Ordinal) &&
            message.Text.Contains("LatestStable", StringComparison.Ordinal));
    }

    [Fact]
    public void Latest_prerelease_policy_selects_highest_version_including_previews()
    {
        var selected = Select(
            PackageSourceVersionDiscoveryPolicy.LatestIncludingPrerelease,
            "3.0.2",
            "3.7.0-preview.4512",
            "2.9.0");

        selected.Should().ContainSingle().Which.Should().Be("3.7.0-preview.4512");
    }

    [Fact]
    public void All_versions_policy_preserves_discovered_versions()
    {
        var selected = Select(
            PackageSourceVersionDiscoveryPolicy.AllVersions,
            "1.0.0",
            "2.0.0-preview.1",
            "2.0.0");

        selected.Should().Equal("1.0.0", "2.0.0-preview.1", "2.0.0");
    }

    private static IReadOnlyList<string> Select(PackageSourceVersionDiscoveryPolicy policy, params string[] versions) =>
        NuGetPackageSourceClient
            .SelectVersions(policy, versions.Select(NuGetVersion.Parse))
            .Select(version => version.ToNormalizedString())
            .ToList();

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<LogMessage> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(new LogMessage(logLevel, formatter(state, exception)));
        }
    }

    private sealed record LogMessage(LogLevel Level, string Text);
}

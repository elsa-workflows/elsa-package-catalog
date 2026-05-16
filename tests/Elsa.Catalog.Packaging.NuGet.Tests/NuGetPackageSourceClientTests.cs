using Elsa.Catalog.Core.Packaging;
using Elsa.Catalog.Core.Packages;
using Elsa.Catalog.Core.Sources;
using FluentAssertions;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Elsa.Catalog.Packaging.NuGet.Tests;

public sealed class NuGetPackageSourceClientTests
{
    [Fact]
    public async Task Prefix_wildcard_sources_discover_matching_package_ids()
    {
        await using var feed = await LoopbackNuGetFeed.StartAsync();
        var client = new NuGetPackageSourceClient(new PackageSourcePatternMatcher());
        var source = new PackageSource
        {
            Url = feed.ServiceIndexUrl,
            IncludePatterns = ["Elsa.*"],
            ExcludePatterns = ["*.Tests"]
        };

        var versions = await client.FindPackageVersionsAsync(source);

        versions.Should().BeEquivalentTo([
            new DiscoveredPackageVersion("Elsa.Email", "1.0.0"),
            new DiscoveredPackageVersion("Elsa.Workflows", "2.0.0")
        ]);
        feed.SearchQueries.Should().Contain("Elsa.");
    }

    [Fact]
    public async Task Leading_wildcard_only_sources_do_not_trigger_broad_feed_crawling()
    {
        var client = new NuGetPackageSourceClient(new PackageSourcePatternMatcher());
        var source = new PackageSource
        {
            Url = "https://example.invalid/v3/index.json",
            IncludePatterns = ["*.Elsa"]
        };

        var act = () => client.FindPackageVersionsAsync(source);

        await act.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*Leading wildcard-only sources are not crawled.*");
    }

    [Fact]
    public async Task Prefix_wildcard_sources_require_feed_search_support()
    {
        await using var feed = await LoopbackNuGetFeed.StartAsync(advertiseSearch: false);
        var client = new NuGetPackageSourceClient(new PackageSourcePatternMatcher());
        var source = new PackageSource
        {
            Url = feed.ServiceIndexUrl,
            IncludePatterns = ["Elsa.*"]
        };

        var act = () => client.FindPackageVersionsAsync(source);

        await act.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*advertises a NuGet search service*");
    }

    private sealed class LoopbackNuGetFeed : IAsyncDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource _stopped = new();
        private readonly bool _advertiseSearch;
        private readonly Task _requests;

        private LoopbackNuGetFeed(bool advertiseSearch)
        {
            _advertiseSearch = advertiseSearch;
            _listener.Start();
            var port = ((IPEndPoint)_listener.LocalEndpoint).Port;
            BaseUrl = $"http://127.0.0.1:{port}/";
            _requests = Task.Run(ProcessRequestsAsync);
        }

        public string BaseUrl { get; }
        public string ServiceIndexUrl => $"{BaseUrl}v3/index.json";
        public List<string> SearchQueries { get; } = [];

        public static Task<LoopbackNuGetFeed> StartAsync(bool advertiseSearch = true) =>
            Task.FromResult(new LoopbackNuGetFeed(advertiseSearch));

        public async ValueTask DisposeAsync()
        {
            await _stopped.CancelAsync();
            _listener.Stop();
            try
            {
                await _requests;
            }
            catch (Exception ex) when (ex is SocketException or ObjectDisposedException or IOException)
            {
            }
            _stopped.Dispose();
        }

        private async Task ProcessRequestsAsync()
        {
            while (!_stopped.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await _listener.AcceptTcpClientAsync(_stopped.Token);
                }
                catch (Exception ex) when (_stopped.IsCancellationRequested && ex is OperationCanceledException or SocketException or ObjectDisposedException)
                {
                    return;
                }

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await RespondAsync(client);
                    }
                    catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException)
                    {
                    }
                }, _stopped.Token);
            }
        }

        private async Task RespondAsync(TcpClient client)
        {
            using var connection = client;
            await using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
            var requestLine = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(requestLine))
                return;

            while (!string.IsNullOrEmpty(await reader.ReadLineAsync()))
            {
            }

            var target = requestLine.Split(' ', StringSplitOptions.RemoveEmptyEntries).ElementAtOrDefault(1) ?? "/";
            var requestUri = new Uri(new Uri(BaseUrl), target);
            var path = requestUri.AbsolutePath;
            var json = path switch
            {
                "/v3/index.json" => ServiceIndexJson(),
                "/query" => SearchJson(GetQueryValue(requestUri, "q")),
                "/flat/elsa.email/index.json" => VersionIndexJson("1.0.0"),
                "/flat/elsa.workflows/index.json" => VersionIndexJson("2.0.0"),
                _ => "{}"
            };

            var status = path == "/flat/elsa.tests/index.json" ? "404 Not Found" : "200 OK";
            var body = Encoding.UTF8.GetBytes(json);
            var header = Encoding.ASCII.GetBytes(
                $"HTTP/1.1 {status}\r\nContent-Type: application/json\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n");
            await stream.WriteAsync(header);
            await stream.WriteAsync(body);
        }

        private string ServiceIndexJson() =>
            $$"""
            {
              "@context": {
                "@vocab": "http://schema.nuget.org/services#",
                "comment": "http://www.w3.org/2000/01/rdf-schema#comment"
              },
              "version": "3.0.0",
              "resources": [
                {{SearchServiceResourceJson()}}
                { "@id": "{{BaseUrl}}flat/", "@type": "PackageBaseAddress/3.0.0" }
              ]
            }
            """;

        private string SearchJson(string query)
        {
            SearchQueries.Add(query);
            return """
            {
              "totalHits": 3,
              "data": [
                { "id": "Elsa.Email", "version": "1.0.0" },
                { "id": "Elsa.Tests", "version": "1.0.0" },
                { "id": "Elsa.Workflows", "version": "2.0.0" }
              ]
            }
            """;
        }

        private string SearchServiceResourceJson() =>
            _advertiseSearch ? $$""" { "@id": "{{BaseUrl}}query", "@type": "SearchQueryService/3.0.0-beta" },""" : "";

        private static string VersionIndexJson(string version) =>
            $$"""
            { "versions": ["{{version}}"] }
            """;

        private static string GetQueryValue(Uri uri, string name)
        {
            var pairs = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in pairs)
            {
                var parts = pair.Split('=', 2);
                if (parts.Length == 2 && parts[0] == name)
                    return Uri.UnescapeDataString(parts[1].Replace('+', ' '));
            }

            return "";
        }
    }
}

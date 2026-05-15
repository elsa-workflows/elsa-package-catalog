using Elsa.Catalog.Core.Packaging;
using Elsa.Catalog.Core.Packages;
using Elsa.Catalog.Core.Sources;
using FluentAssertions;
using System.Net;
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

    private sealed class LoopbackNuGetFeed : IAsyncDisposable
    {
        private readonly HttpListener _listener = new();
        private readonly CancellationTokenSource _stopped = new();
        private readonly Task _requests;

        private LoopbackNuGetFeed(string baseUrl)
        {
            BaseUrl = baseUrl;
            _listener.Prefixes.Add(BaseUrl);
            _listener.Start();
            _requests = Task.Run(ProcessRequestsAsync);
        }

        public string BaseUrl { get; }
        public string ServiceIndexUrl => $"{BaseUrl}v3/index.json";
        public List<string> SearchQueries { get; } = [];

        public static Task<LoopbackNuGetFeed> StartAsync()
        {
            var port = FreeTcpPort();
            return Task.FromResult(new LoopbackNuGetFeed($"http://127.0.0.1:{port}/"));
        }

        public async ValueTask DisposeAsync()
        {
            await _stopped.CancelAsync();
            _listener.Stop();
            try
            {
                await _requests;
            }
            catch (Exception ex) when (ex is HttpListenerException or ObjectDisposedException)
            {
            }
            _listener.Close();
            _stopped.Dispose();
        }

        private async Task ProcessRequestsAsync()
        {
            while (!_stopped.IsCancellationRequested)
            {
                HttpListenerContext context;
                try
                {
                    context = await _listener.GetContextAsync();
                }
                catch (Exception ex) when (_stopped.IsCancellationRequested && ex is HttpListenerException or ObjectDisposedException)
                {
                    return;
                }

                await RespondAsync(context);
            }
        }

        private async Task RespondAsync(HttpListenerContext context)
        {
            var path = context.Request.Url!.AbsolutePath;
            var json = path switch
            {
                "/v3/index.json" => ServiceIndexJson(),
                "/query" => SearchJson(context.Request.QueryString["q"] ?? ""),
                "/flat/elsa.email/index.json" => VersionIndexJson("1.0.0"),
                "/flat/elsa.workflows/index.json" => VersionIndexJson("2.0.0"),
                _ => "{}"
            };

            context.Response.StatusCode = path == "/flat/elsa.tests/index.json" ? 404 : 200;
            context.Response.ContentType = "application/json";
            var body = Encoding.UTF8.GetBytes(json);
            context.Response.ContentLength64 = body.Length;
            await context.Response.OutputStream.WriteAsync(body);
            context.Response.Close();
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
                { "@id": "{{BaseUrl}}query", "@type": "SearchQueryService/3.0.0-beta" },
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

        private static string VersionIndexJson(string version) =>
            $$"""
            { "versions": ["{{version}}"] }
            """;

        private static int FreeTcpPort()
        {
            var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}

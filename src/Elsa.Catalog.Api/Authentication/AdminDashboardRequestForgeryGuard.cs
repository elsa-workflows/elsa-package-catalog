namespace Elsa.Catalog.Api.Authentication;

public static class AdminDashboardRequestForgeryGuard
{
    private static readonly StringComparer HeaderComparer = StringComparer.OrdinalIgnoreCase;

    public static bool IsSameOriginPost(HttpRequest request)
    {
        if (!HttpMethods.IsPost(request.Method))
            return false;

        if (request.Headers.TryGetValue("Sec-Fetch-Site", out var fetchSite) &&
            fetchSite.Any(value => HeaderComparer.Equals(value, "cross-site") || HeaderComparer.Equals(value, "none")))
        {
            return false;
        }

        if (request.Headers.TryGetValue("Origin", out var origins))
            return origins.Any(value => IsSameOrigin(value, request));

        if (request.Headers.TryGetValue("Referer", out var referers))
            return referers.Any(value => IsSameOrigin(value, request));

        return false;
    }

    private static bool IsSameOrigin(string? value, HttpRequest request)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            return false;

        return HeaderComparer.Equals(uri.Scheme, request.Scheme) &&
               HeaderComparer.Equals(uri.Host, request.Host.Host) &&
               uri.Port == GetPort(request);
    }

    private static int GetPort(HttpRequest request)
    {
        if (request.Host.Port is { } port)
            return port;

        return HeaderComparer.Equals(request.Scheme, "https") ? 443 : 80;
    }
}

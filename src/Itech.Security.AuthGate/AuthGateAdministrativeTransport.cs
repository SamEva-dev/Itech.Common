using Microsoft.Extensions.DependencyInjection;

namespace Itech.Security.AuthGate;

/// <summary>Opt-in transport for existing privileged clients. Origins must come from trusted
/// server configuration. Does not acquire credentials or replace application authorization.</summary>
public static class AuthGateAdministrativeTransport
{
    public static IHttpClientBuilder WithAuthGateAdministrativeTransport(this IHttpClientBuilder builder,
        IEnumerable<string> allowedOrigins, int maxBufferedResponseBytes = 16 * 1024 * 1024)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(allowedOrigins);
        if (maxBufferedResponseBytes is < 1024 or > 64 * 1024 * 1024)
            throw new ArgumentOutOfRangeException(nameof(maxBufferedResponseBytes));
        var origins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in allowedOrigins)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || !AuthGateServiceUri.IsHttps(uri) ||
                uri.AbsolutePath != "/" || uri.Query.Length != 0)
                throw new InvalidOperationException("Administrative transport requires configured HTTPS origins without a path or query.");
            origins.Add(uri.GetLeftPart(UriPartial.Authority));
        }
        if (origins.Count is < 1 or > 32)
            throw new InvalidOperationException("Administrative transport requires 1 to 32 configured origins.");

        return builder.ConfigureHttpClient(client => client.MaxResponseContentBufferSize = maxBufferedResponseBytes)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false,
                UseCookies = false
            })
            .AddHttpMessageHandler(() => new AdministrativeOriginGuard(origins))
            .WithAuthGateTransport("administrative." + builder.Name, retryReads: false);
    }

    private sealed class AdministrativeOriginGuard(HashSet<string> origins) : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (request.RequestUri is not { IsAbsoluteUri: true } uri || !AuthGateServiceUri.IsHttps(uri) ||
                !origins.Contains(uri.GetLeftPart(UriPartial.Authority)) || request.Headers.Host is not null)
                throw new AuthGateTransportException(AuthGateTransportError.TransportUnavailable);
            return base.SendAsync(request, ct);
        }
    }
}

using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Itech.Security.AuthGate;

/// <summary>Transport for server-side enrollment only. The authority must be trusted server configuration;
/// the bearer must come from the already authenticated OIDC request. AuthGate validates it again.
/// Business authorization, response validation and idempotency remain the caller's responsibility.</summary>
public static class AuthGateRegistrationTransport
{
    public const string ClientName = "Itech.Security.AuthGate.Registration";
    private const int MaxResponseBytes = 16 * 1024;
    private sealed class RegistrationMarker { }

    public static IServiceCollection AddAuthGateRegistrationTransport(this IServiceCollection services)
    {
        if (services.Any(s => s.ServiceType == typeof(RegistrationMarker))) return services;
        services.AddSingleton(new RegistrationMarker());
        services.AddHttpClient(ClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(8);
            client.MaxResponseContentBufferSize = MaxResponseBytes;
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = false, UseCookies = false
        }).WithAuthGateTransport("registration"); // No automatic retries, including enrollment GETs.
        return services;
    }

    public static async Task<HttpResponseMessage> SendAsync(IHttpClientFactory factory, Uri authority,
        string operation, string? bearer, string? internalKey, object? payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(factory);
        cancellationToken.ThrowIfCancellationRequested();
        if (!AuthGateServiceUri.IsHttps(authority) || authority.AbsolutePath != "/" || authority.Query.Length != 0)
            throw InvalidRequest();

        var method = operation switch
        {
            "api/oidc/registration/context" or "api/oidc/registration/verified-identity"
                when payload is null => HttpMethod.Get,
            "api/oidc/registration/organization-enroll" when payload is not null => HttpMethod.Post,
            "api/oidc/registration/self-enroll" when payload is null => HttpMethod.Post,
            "internal/application-access/ensure" when payload is not null => HttpMethod.Post,
            _ => throw InvalidRequest()
        };
        var internalOperation = operation == "internal/application-access/ensure";
        if (internalOperation)
        {
            if (bearer is not null || !ValidCredential(internalKey, 32, 4096)) throw InvalidRequest();
        }
        else if (internalKey is not null || !ValidCredential(bearer, 1, 16384)) throw InvalidRequest();

        using var request = new HttpRequestMessage(method, new Uri(authority, operation));
        request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true, NoStore = true };
        if (internalOperation) request.Headers.Add("X-Internal-Api-Key", internalKey!);
        else request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer!);
        if (payload is not null) request.Content = JsonContent.Create(payload);
        using var client = factory.CreateClient(ClientName);
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        budget.CancelAfter(TimeSpan.FromSeconds(8));
        HttpResponseMessage? response = null;
        try
        {
            // Buffer the body inside the deadline and enforce the limit even with an injected client.
            response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, budget.Token);
            var bytes = await AuthGateServiceUri.ReadBoundedAsync(response.Content, MaxResponseBytes, budget.Token);
            var original = response.Content;
            var content = new ByteArrayContent(bytes);
            foreach (var header in original.Headers)
                if (!header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                    content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            response.Content = content;
            original.Dispose();
            var result = response;
            response = null;
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (OperationCanceledException) { throw new AuthGateTransportException(AuthGateTransportError.Timeout); }
        catch (AuthGateTransportException) { throw; }
        catch (Exception e) when (e is HttpRequestException or IOException)
        { throw new AuthGateTransportException(AuthGateTransportError.TransportUnavailable); }
        finally { response?.Dispose(); }
    }

    private static bool ValidCredential(string? value, int min, int max) =>
        value is not null && value.Length >= min && value.Length <= max &&
        !value.Any(c => char.IsWhiteSpace(c) || char.IsControl(c) || c == ',');

    private static HttpRequestException InvalidRequest() => new("Invalid AuthGate registration transport request.");
}

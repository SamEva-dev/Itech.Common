using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Itech.Security.AuthGate;

/// <summary>Explicit destination for a single operation, constructed only from trusted server configuration.
/// Never accept an endpoint, audience, scope or credential from a browser or request body.</summary>
public sealed record AuthGateMachineEndpoint(AuthGateMachineSettings TokenSettings, Uri Endpoint);

public sealed class AuthGateServiceClient(IHttpClientFactory factory, AuthGateMachineTokenClient tokens)
{
    internal const string HttpClientName = "Itech.Security.AuthGate.Service";
    internal const string InternalHttpClientName = "Itech.Security.AuthGate.InternalKey";
    private const int MaxResponseBytes = 1024 * 1024;

    /// <summary>No automatic retries. Caller must dispose the buffered response and handle all HTTP statuses.
    /// A failed token acquisition never sends a downstream request.</summary>
    public async Task<HttpResponseMessage> SendMachineAsync(AuthGateMachineEndpoint binding, string clientSecret,
        HttpMethod method, object? jsonBody = null, string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(binding);
        binding.TokenSettings.Validate();
        ValidateRequest(binding.Endpoint, method, jsonBody, idempotencyKey);
        // One end-to-end budget for token acquisition AND downstream response buffering.
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        budget.CancelAfter(TimeSpan.FromSeconds(binding.TokenSettings.TimeoutSeconds));
        try
        {
            var result = await tokens.AcquireAsync(binding.TokenSettings, clientSecret, budget.Token);
            if (result.State != AuthGateMachineTokenState.Acquired || result.AccessToken is null)
                throw new AuthGateMachineCredentialException(result.State);
            return await SendAsync(HttpClientName, binding.Endpoint, method, jsonBody, idempotencyKey,
                request => request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken),
                binding.TokenSettings.TimeoutSeconds, budget.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && budget.IsCancellationRequested)
        {
            throw new AuthGateTransportException(AuthGateTransportError.Timeout);
        }
    }

    /// <summary>Scoped key compatibility for AuthGate's internal application-access API only.
    /// Server still enforces the application and operation policy. No key is sent to arbitrary service origins.</summary>
    public Task<HttpResponseMessage> SendInternalKeyAsync(Uri authority, string operation, string apiKey,
        HttpMethod method, object? jsonBody = null, string? query = null, CancellationToken cancellationToken = default)
    {
        if (!AuthGateServiceUri.IsHttps(authority) || authority.AbsolutePath != "/" || authority.Query.Length != 0 ||
            string.IsNullOrWhiteSpace(apiKey) || apiKey.Length is < 32 or > 4096 || apiKey.Any(c => char.IsWhiteSpace(c) || char.IsControl(c)))
            throw new ArgumentException("Invalid scoped internal credential configuration.");
        var expectedMethod = operation switch
        {
            "identity-status" or "ensure" or "catalog:sync" => HttpMethod.Post,
            "role" or "membership-status" => HttpMethod.Put,
            "users" or "roles" or "membership-state" => HttpMethod.Get,
            _ => throw new ArgumentException("Unsupported internal application-access operation.", nameof(operation))
        };
        if (method != expectedMethod || (query is not null && (method != HttpMethod.Get ||
            query.Length > 8192 || !query.StartsWith('?') || query.Contains('#') || query.Any(char.IsControl))))
            throw new ArgumentException("Invalid internal operation request.");
        var endpoint = new Uri(authority, "/internal/application-access/" + operation + query);
        ValidateRequest(endpoint, method, jsonBody, null);
        return SendAsync(InternalHttpClientName, endpoint, method, jsonBody, null,
            request => request.Headers.Add("X-Internal-Api-Key", apiKey), 10, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAsync(string clientName, Uri endpoint, HttpMethod method,
        object? body, string? idempotencyKey, Action<HttpRequestMessage> authenticate, int seconds, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(seconds));
        using var client = factory.CreateClient(clientName);
        using var request = new HttpRequestMessage(method, endpoint);
        if (body is not null) request.Content = JsonContent.Create(body);
        if (idempotencyKey is not null) request.Headers.Add("Idempotency-Key", idempotencyKey);
        request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true, NoStore = true };
        authenticate(request);
        // ResponseContentRead buffers under the named client's 1 MiB limit, including unknown Content-Length.
        // Response must be disposed by the caller. No redirect, retry, or user-supplied headers.
        try
        {
            return await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, timeout.Token);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (OperationCanceledException) { throw new AuthGateTransportException(AuthGateTransportError.Timeout); }
        catch (AuthGateTransportException) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or IOException)
        {
            throw new AuthGateTransportException(AuthGateTransportError.TransportUnavailable);
        }
    }

    private static void ValidateRequest(Uri endpoint, HttpMethod method, object? body, string? key)
    {
        if (!AuthGateServiceUri.IsHttps(endpoint) || method is null ||
            (method != HttpMethod.Get && method != HttpMethod.Post && method != HttpMethod.Put &&
             method != HttpMethod.Patch && method != HttpMethod.Delete) || (method == HttpMethod.Get && body is not null) ||
            (key is not null && (key.Length is < 1 or > 200 || key.Any(c => !(char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.')))))
            throw new ArgumentException("Invalid AuthGate service request.");
    }

    /// <summary>Can be registered independently of the browser OIDC authentication scheme.</summary>
    internal static IServiceCollection AddClients(IServiceCollection services)
    {
        if (services.Any(x => x.ServiceType == typeof(RegistrationMarker))) return services;
        services.AddSingleton(new RegistrationMarker());
        services.TryAddSingleton<AuthGateMachineTokenClient>();
        services.TryAddSingleton<AuthGateServiceClient>();
        Register(AuthGateMachineTokenClient.HttpClientName, 32 * 1024, "machine_token");
        Register(HttpClientName, MaxResponseBytes, "service");
        Register(InternalHttpClientName, MaxResponseBytes, "internal_key");
        return services;

        void Register(string name, int maxBytes, string purpose)
        {
            services.AddHttpClient(name, client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.MaxResponseContentBufferSize = maxBytes;
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false, UseCookies = false
            }).WithAuthGateTransport(purpose);
        }
    }
    private sealed class RegistrationMarker { }
}

public sealed class AuthGateMachineCredentialException(AuthGateMachineTokenState state)
    : InvalidOperationException("AuthGate machine token acquisition failed.")
{
    public AuthGateMachineTokenState State { get; } = state;
}

internal static class AuthGateServiceUri
{
    internal static bool IsHttps(Uri? uri) => uri is { IsAbsoluteUri: true } && uri.Scheme == Uri.UriSchemeHttps &&
        uri.Host.Length > 0 && uri.UserInfo.Length == 0 && uri.Fragment.Length == 0;

    internal static async Task<byte[]> ReadBoundedAsync(HttpContent content, int maximum, CancellationToken ct)
    {
        if (content.Headers.ContentLength > maximum) throw new IOException("AuthGate response exceeds limit.");
        await using var stream = await content.ReadAsStreamAsync(ct);
        using var buffer = new MemoryStream();
        var block = new byte[8192];
        int count;
        while ((count = await stream.ReadAsync(block.AsMemory(), ct)) != 0)
        {
            if (buffer.Length + count > maximum) throw new IOException("AuthGate response exceeds limit.");
            buffer.Write(block, 0, count);
        }
        return buffer.ToArray();
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Itech.Security.AuthGate;

public enum AuthGateSessionState { Active, Rejected, Unavailable }

/// <summary>Live check; no caching, no retries, no forwarding of service credentials.</summary>
public sealed class AuthGateSessionValidator(IHttpClientFactory factory, ILogger<AuthGateSessionValidator> logger)
{
    internal const string HttpClientName = "Itech.Security.AuthGate.Session";

    public async Task<AuthGateSessionState> ValidateAsync(AuthGateOidcSettings settings,
        string authorization, CancellationToken cancellationToken = default)
    {
        settings.Validate();
        if (!AuthenticationHeaderValue.TryParse(authorization, out var header) ||
            !string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(header.Parameter) || header.Parameter.Any(char.IsWhiteSpace))
            return AuthGateSessionState.Rejected;
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(settings.SessionTimeoutSeconds));
            using var client = factory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Get, settings.SessionEndpoint);
            request.Headers.Authorization = header;
            request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true, NoStore = true };
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, timeout.Token);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return AuthGateSessionState.Rejected;
            if (response.StatusCode != HttpStatusCode.OK) return AuthGateSessionState.Unavailable;
            using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: timeout.Token);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return AuthGateSessionState.Unavailable;
            var active = document.RootElement.EnumerateObject().Where(p => p.NameEquals("active")).ToArray();
            if (active.Length != 1) return AuthGateSessionState.Unavailable;
            return active[0].Value.ValueKind switch
            {
                JsonValueKind.True => AuthGateSessionState.Active,
                JsonValueKind.False => AuthGateSessionState.Rejected,
                _ => AuthGateSessionState.Unavailable
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException or IOException)
        {
            logger.LogWarning("AuthGate session validation unavailable ({ErrorType}).", ex.GetType().Name);
            return AuthGateSessionState.Unavailable;
        }
    }
}

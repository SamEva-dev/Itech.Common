using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Itech.Security.AuthGate;

public enum AuthGateSessionState { Active, Rejected, Unavailable }

/// <summary>Live check; no caching or forwarding of service credentials. Transport may retry a transient GET.</summary>
public sealed class AuthGateSessionValidator(IHttpClientFactory factory, ILogger<AuthGateSessionValidator> logger)
{
    internal const string HttpClientName = "Itech.Security.AuthGate.Session";

    public async Task<AuthGateSessionState> ValidateAsync(AuthGateOidcSettings settings,
        string authorization, CancellationToken cancellationToken = default) =>
        ValidateCoreAsync(settings, authorization, null, cancellationToken).Result;

    public Task<AuthGateSessionState> ValidatePlatformAsync(AuthGateOidcSettings settings,
        string authorization, Guid subject, CancellationToken cancellationToken = default) =>
        subject == Guid.Empty || settings.AllowMissingClientBinding
            ? Task.FromResult(AuthGateSessionState.Rejected)
            : ValidateCoreAsync(settings, authorization, subject, cancellationToken);

    private async Task<AuthGateSessionState> ValidateCoreAsync(AuthGateOidcSettings settings,
        string authorization, Guid? platformSubject, CancellationToken cancellationToken)
    {
        settings.Validate();
        if (!AuthenticationHeaderValue.TryParse(authorization, out var header) ||
            !string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(header.Parameter) || header.Parameter.Length > 16 * 1024 || header.Parameter.Any(c => char.IsWhiteSpace(c) || char.IsControl(c)))
            return AuthGateSessionState.Rejected;
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(settings.SessionTimeoutSeconds));
            using var client = factory.CreateClient(HttpClientName);
            var endpoint = platformSubject.HasValue
                ? new Uri(new Uri(settings.Issuer, UriKind.Absolute), "/api/oidc/platform/session/validate")
                : settings.SessionEndpoint;
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Authorization = header;
            request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true, NoStore = true };
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, timeout.Token);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return AuthGateSessionState.Rejected;
            if (response.StatusCode != HttpStatusCode.OK) return AuthGateSessionState.Unavailable;
            if (!string.Equals(response.Content.Headers.ContentType?.MediaType, "application/json", StringComparison.OrdinalIgnoreCase))
                return AuthGateSessionState.Unavailable;
            var bytes = await AuthGateServiceUri.ReadBoundedAsync(response.Content, 16 * 1024, timeout.Token);
            using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 8 });
            if (document.RootElement.ValueKind != JsonValueKind.Object) return AuthGateSessionState.Unavailable;
            var active = document.RootElement.EnumerateObject().Where(p => p.NameEquals("active")).ToArray();
            if (active.Length != 1) return AuthGateSessionState.Unavailable;
            if (platformSubject.HasValue && active[0].Value.ValueKind == JsonValueKind.True)
            {
                var subjects = document.RootElement.EnumerateObject()
                    .Where(p => string.Equals(p.Name, "subject", StringComparison.OrdinalIgnoreCase)).ToArray();
                if (subjects.Length != 1 || !subjects[0].NameEquals("subject") ||
                    subjects[0].Value.ValueKind != JsonValueKind.String ||
                    !Guid.TryParse(subjects[0].Value.GetString(), out var actual) || actual != platformSubject.Value)
                    return AuthGateSessionState.Unavailable;
                if (document.RootElement.EnumerateObject().Count(p =>
                    string.Equals(p.Name, "active", StringComparison.OrdinalIgnoreCase)) != 1)
                    return AuthGateSessionState.Unavailable;
            }
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

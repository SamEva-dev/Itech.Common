using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Itech.Security.AuthGate;

/// <summary>Trusted server configuration only. Legacy machine JWT issuer may differ from the OIDC issuer.</summary>
public sealed record AuthGateMachineSettings(string Authority, string TokenIssuer, string ClientId,
    string Scope, string Audience, int TimeoutSeconds = 5)
{
    public static AuthGateMachineSettings FromConfiguration(IConfiguration section) => new(
        section["Authority"] ?? "", section["TokenIssuer"] ?? "", section["ClientId"] ?? "",
        section["Scope"] ?? "", section["Audience"] ?? "", section.GetValue("TimeoutSeconds", 5));

    public void Validate()
    {
        if (!Uri.TryCreate(Authority, UriKind.Absolute, out var uri) || !AuthGateServiceUri.IsHttps(uri) ||
            uri.AbsolutePath != "/" || uri.Query.Length != 0 || !ValidCode(TokenIssuer) ||
            !ValidCode(ClientId) || ClientId.Contains(':') || !ValidCode(Scope) || Scope.Contains(':') ||
            !ValidCode(Audience) || TimeoutSeconds is < 1 or > 30)
            throw new InvalidOperationException("Invalid AuthGate machine client configuration.");
    }

    internal static bool ValidCode(string value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 512 &&
        !value.Any(c => char.IsWhiteSpace(c) || char.IsControl(c));
}

public enum AuthGateMachineTokenState { Acquired, Denied, Unavailable }

// Deliberately not a record: its generated ToString must never print the bearer token.
public sealed class AuthGateMachineTokenResult
{
    internal AuthGateMachineTokenResult(AuthGateMachineTokenState state, string? token = null)
        => (State, AccessToken) = (state, token);
    public AuthGateMachineTokenState State { get; }
    public string? AccessToken { get; }
}

/// <summary>Acquires the existing AuthGate machine JWT via authenticated TLS. No cache or retry.
/// Claim inspection is a consistency check, NOT cryptographic validation; the recipient must validate the JWT.</summary>
public sealed class AuthGateMachineTokenClient(IHttpClientFactory factory, ILogger<AuthGateMachineTokenClient> logger)
{
    internal const string HttpClientName = "Itech.Security.AuthGate.MachineToken";
    private const int MaxResponseBytes = 32 * 1024;

    public async Task<AuthGateMachineTokenResult> AcquireAsync(AuthGateMachineSettings settings,
        string clientSecret, CancellationToken cancellationToken = default)
    {
        settings.Validate();
        if (string.IsNullOrWhiteSpace(clientSecret) || clientSecret.Length > 4096 || clientSecret.Any(char.IsControl))
            throw new ArgumentException("Invalid machine credential.", nameof(clientSecret));
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds));
            using var client = factory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(settings.Authority), "/api/m2m/token"));
            request.Content = JsonContent.Create(new { clientId = settings.ClientId, clientSecret, scope = settings.Scope });
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true, NoStore = true };
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return new(AuthGateMachineTokenState.Denied);
            if (response.StatusCode != HttpStatusCode.OK ||
                !string.Equals(response.Content.Headers.ContentType?.MediaType, "application/json", StringComparison.OrdinalIgnoreCase))
                return new(AuthGateMachineTokenState.Unavailable);
            var bytes = await AuthGateServiceUri.ReadBoundedAsync(response.Content, MaxResponseBytes, timeout.Token);
            using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 8 });
            var root = document.RootElement;
            var token = Required(root, "accessToken").GetString();
            var lifetime = Required(root, "expiresIn").GetInt32();
            if (token is null || lifetime is < 1 or > 300 || !Matches(token, settings, lifetime, DateTimeOffset.UtcNow))
                return new(AuthGateMachineTokenState.Unavailable);
            return new(AuthGateMachineTokenState.Acquired, token);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException or IOException
            or InvalidOperationException or FormatException or ArgumentException or OverflowException)
        {
            logger.LogWarning("AuthGate machine token acquisition unavailable ({ErrorType}).", ex.GetType().Name);
            return new(AuthGateMachineTokenState.Unavailable);
        }
    }

    internal static bool Matches(string token, AuthGateMachineSettings settings, int lifetime, DateTimeOffset now)
    {
        if (token.Length > 16 * 1024 || token.Any(c => !(char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.'))) return false;
        var parts = token.Split('.');
        if (parts.Length != 3 || parts.Any(string.IsNullOrEmpty)) return false;
        using var header = JsonDocument.Parse(Base64UrlEncoder.DecodeBytes(parts[0]), new JsonDocumentOptions { MaxDepth = 8 });
        using var body = JsonDocument.Parse(Base64UrlEncoder.DecodeBytes(parts[1]), new JsonDocumentOptions { MaxDepth = 8 });
        var payload = body.RootElement;
        if (Required(header.RootElement, "alg").GetString() != "RS256" ||
            Required(payload, "iss").GetString() != settings.TokenIssuer ||
            Required(payload, "sub").GetString() != settings.ClientId ||
            Required(payload, "azp").GetString() != settings.ClientId ||
            Required(payload, "scope").GetString() != settings.Scope ||
            !Guid.TryParse(Required(payload, "jti").GetString(), out var id) || id == Guid.Empty)
            return false;
        var audience = Required(payload, "aud");
        var actualAudience = audience.ValueKind == JsonValueKind.Array && audience.GetArrayLength() == 1
            ? audience[0].GetString() : audience.ValueKind == JsonValueKind.String ? audience.GetString() : null;
        if (actualAudience != settings.Audience) return false;
        var expiry = Required(payload, "exp").GetInt64();
        var seconds = now.ToUnixTimeSeconds();
        // Small upper-bound tolerance for clock differences; never forward a locally expired token.
        if (expiry <= seconds || expiry > seconds + lifetime + 5) return false;
        var notBefore = payload.EnumerateObject().Where(p => p.NameEquals("nbf")).ToArray();
        return notBefore.Length == 0 || (notBefore.Length == 1 && notBefore[0].Value.GetInt64() <= seconds);
    }

    private static JsonElement Required(JsonElement root, string name)
    {
        if (root.ValueKind != JsonValueKind.Object) throw new JsonException("Invalid object.");
        // Reject case aliases as well as exact duplicates, rather than accepting ambiguous responses.
        var matches = root.EnumerateObject().Where(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (matches.Length != 1 || !matches[0].NameEquals(name)) throw new JsonException("Missing or ambiguous property.");
        return matches[0].Value;
    }
}

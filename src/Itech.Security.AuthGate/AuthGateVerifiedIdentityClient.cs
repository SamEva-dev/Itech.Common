using System.Net;
using System.Text.Json;

namespace Itech.Security.AuthGate;

public enum AuthGateVerifiedIdentityState { Resolved, Denied, Unavailable }
public sealed record AuthGateVerifiedIdentityResult(AuthGateVerifiedIdentityState State, string? Email = null);

/// <summary>Reads the verified email of an already authenticated OIDC subject. The origin and audience
/// are trusted server configuration, not user input. This is not an invitation authorization decision.</summary>
public static class AuthGateVerifiedIdentityClient
{
    public static async Task<AuthGateVerifiedIdentityResult> ResolveAsync(IHttpClientFactory factory,
        Uri authority, string bearer, Guid expectedSubject, string expectedAudience,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (expectedSubject == Guid.Empty || string.IsNullOrWhiteSpace(expectedAudience))
            return new(AuthGateVerifiedIdentityState.Unavailable);
        try
        {
            using var response = await AuthGateRegistrationTransport.SendAsync(factory, authority,
                "api/oidc/registration/verified-identity", bearer, null, null, cancellationToken);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return new(AuthGateVerifiedIdentityState.Denied);
            if (response.StatusCode != HttpStatusCode.OK ||
                !string.Equals(response.Content.Headers.ContentType?.MediaType, "application/json", StringComparison.OrdinalIgnoreCase))
                return new(AuthGateVerifiedIdentityState.Unavailable);
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var json = await JsonDocument.ParseAsync(stream, new JsonDocumentOptions { MaxDepth = 8 }, cancellationToken);
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return new(AuthGateVerifiedIdentityState.Unavailable);
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in root.EnumerateObject())
                if (!names.Add(property.Name)) return new(AuthGateVerifiedIdentityState.Unavailable);
            if (!ReadString(root, "subject", out var subjectText) || !Guid.TryParse(subjectText, out var subject) ||
                !ReadString(root, "audience", out var audience) || !ReadString(root, "email", out var email) ||
                string.IsNullOrWhiteSpace(email) || email.Length > 320 || email.Any(char.IsControl))
                return new(AuthGateVerifiedIdentityState.Unavailable);
            if (subject != expectedSubject || !string.Equals(audience, expectedAudience, StringComparison.Ordinal))
                return new(AuthGateVerifiedIdentityState.Denied);
            return new(AuthGateVerifiedIdentityState.Resolved, email.Trim());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or IOException or JsonException or InvalidOperationException or ArgumentException or OperationCanceledException)
        { return new(AuthGateVerifiedIdentityState.Unavailable); }
    }

    private static bool ReadString(JsonElement root, string name, out string value)
    {
        value = string.Empty;
        if (!root.TryGetProperty(name, out var item) || item.ValueKind != JsonValueKind.String) return false;
        value = item.GetString()!;
        return true;
    }
}

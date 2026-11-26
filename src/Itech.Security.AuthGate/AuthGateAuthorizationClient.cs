using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;

namespace Itech.Security.AuthGate;

public enum AuthGateAuthorizationState { Resolved, Denied, Conflict, Unavailable }

public sealed record AuthGatePermissionSnapshot(Guid Subject, string ApplicationCode, Guid? OrganizationId,
    int Version, IReadOnlyList<string> Permissions, IReadOnlyList<string> ExplicitAllowOverrides,
    IReadOnlyList<string> ContextualScopes);

/// <summary>Discovery metadata only. InitialRole is not an authorization grant.</summary>
public sealed record AuthGateMembership(Guid? OrganizationId, string? InitialRole);
public sealed record AuthGateMembershipSnapshot(Guid Subject, string ApplicationCode, string ClientId,
    IReadOnlyList<AuthGateMembership> Memberships);

public sealed record AuthGateAuthorizationResult<T>(AuthGateAuthorizationState State, T? Value) where T : class;

/// <summary>Read-only calls using the access token retained in a successful OIDC authentication ticket.
/// The application code, scheme and settings must be supplied by trusted server configuration.
/// Never treats discovery or transport errors as authorization grants.</summary>
public sealed class AuthGateAuthorizationClient(IHttpClientFactory factory, ILogger<AuthGateAuthorizationClient> logger)
{
    internal const string HttpClientName = "Itech.Security.AuthGate.Authorization";
    private const int MaxResponseBytes = 64 * 1024;

    public Task<AuthGateAuthorizationResult<AuthGatePermissionSnapshot>> ResolvePermissionsAsync(
        AuthenticateResult authentication, AuthGateOidcSettings settings, string applicationCode,
        Guid? organizationId, int version = 1, CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty || version is not (1 or 2) || (version == 2 && organizationId is null))
            return Task.FromResult(new AuthGateAuthorizationResult<AuthGatePermissionSnapshot>(AuthGateAuthorizationState.Denied, null));
        var path = "/api/oidc/permissions/effective?version=" + version +
            (organizationId.HasValue ? "&organizationId=" + organizationId.Value.ToString("D") : "");
        return ReadAsync(authentication, settings, applicationCode, path,
            (root, context) => ParsePermissions(root, context, applicationCode, organizationId, version), cancellationToken);
    }

    public Task<AuthGateAuthorizationResult<AuthGateMembershipSnapshot>> ListMembershipsAsync(
        AuthenticateResult authentication, AuthGateOidcSettings settings, string applicationCode,
        CancellationToken cancellationToken = default) =>
        ReadAsync(authentication, settings, applicationCode, "/api/oidc/memberships",
            (root, context) => ParseMemberships(root, context, applicationCode), cancellationToken);

    private async Task<AuthGateAuthorizationResult<T>> ReadAsync<T>(AuthenticateResult authentication,
        AuthGateOidcSettings settings, string applicationCode, string path,
        Func<JsonElement, AuthGateTokenContext, T> parse, CancellationToken ct) where T : class
    {
        settings.Validate();
        if (!ValidCode(applicationCode, 128)) throw new ArgumentException("Invalid application code.", nameof(applicationCode));
        var context = authentication.Succeeded && authentication.Principal is not null
            ? AuthGateTokenContext.Resolve(authentication.Principal, settings) : null;
        var token = authentication.Properties?.GetTokenValue("access_token");
        if (context is null || string.IsNullOrWhiteSpace(token) || token.Length > 16 * 1024 ||
            token.Any(c => char.IsWhiteSpace(c) || char.IsControl(c))) return new(AuthGateAuthorizationState.Denied, null);
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(settings.SessionTimeoutSeconds));
            using var client = factory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri(settings.Issuer), path));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true, NoStore = true };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return new(AuthGateAuthorizationState.Denied, null);
            if (response.StatusCode == HttpStatusCode.Conflict) return new(AuthGateAuthorizationState.Conflict, null);
            if (response.StatusCode != HttpStatusCode.OK || response.Content.Headers.ContentLength > MaxResponseBytes ||
                !string.Equals(response.Content.Headers.ContentType?.MediaType, "application/json", StringComparison.OrdinalIgnoreCase))
                return new(AuthGateAuthorizationState.Unavailable, null);
            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var bounded = new MemoryStream();
            var buffer = new byte[8192];
            int read;
            while ((read = await stream.ReadAsync(buffer.AsMemory(), timeout.Token)) != 0)
            {
                if (bounded.Length + read > MaxResponseBytes) return new(AuthGateAuthorizationState.Unavailable, null);
                bounded.Write(buffer, 0, read);
            }
            using var json = JsonDocument.Parse(bounded.ToArray(), new JsonDocumentOptions { MaxDepth = 12 });
            return new(AuthGateAuthorizationState.Resolved, parse(json.RootElement, context));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException or IOException or InvalidOperationException)
        {
            logger.LogWarning("AuthGate authorization resolution unavailable ({ErrorType}).", ex.GetType().Name);
            return new(AuthGateAuthorizationState.Unavailable, null);
        }
    }

    private static AuthGatePermissionSnapshot ParsePermissions(JsonElement root, AuthGateTokenContext context,
        string app, Guid? org, int version)
    {
        VerifyScope(root, context, app);
        var v = Required(root, "version");
        if (ReadGuid(Required(root, "organizationId"), true) != org ||
            Required(root, "permissionsResolved").ValueKind != JsonValueKind.True ||
            v.ValueKind != JsonValueKind.Number || !v.TryGetInt32(out var actual) || actual != version)
            throw new JsonException("Invalid permission context.");
        var permissions = ReadCodes(root, "permissions");
        var allows = ReadCodes(root, "explicitAllowOverrides");
        var scopes = ReadCodes(root, "contextualScopes");
        if (allows.Except(permissions, StringComparer.OrdinalIgnoreCase).Any() ||
            (version == 2 && (allows.Count != 0 || scopes.Count != 0))) throw new JsonException("Invalid grant set.");
        return new(context.UserId, app, org, version, permissions, allows, scopes);
    }

    private static AuthGateMembershipSnapshot ParseMemberships(JsonElement root, AuthGateTokenContext context, string app)
    {
        VerifyScope(root, context, app);
        if (Required(root, "clientId").GetString() != context.ClientId) throw new JsonException("Invalid client.");
        var items = Required(root, "memberships");
        if (items.ValueKind != JsonValueKind.Array || items.GetArrayLength() > 2048) throw new JsonException("Invalid membership list.");
        var memberships = new List<AuthGateMembership>();
        var seen = new HashSet<Guid?>();
        foreach (var item in items.EnumerateArray())
        {
            var org = ReadGuid(Required(item, "organizationId"), true);
            var role = Required(item, "initialRole").GetString();
            if ((role is not null && !ValidCode(role, 256)) || !seen.Add(org)) throw new JsonException("Invalid membership.");
            memberships.Add(new(org, role));
        }
        return new(context.UserId, app, context.ClientId, memberships.AsReadOnly());
    }

    private static void VerifyScope(JsonElement root, AuthGateTokenContext context, string app)
    {
        if (ReadGuid(Required(root, "subject"), false) != context.UserId ||
            !string.Equals(Required(root, "applicationCode").GetString(), app, StringComparison.Ordinal))
            throw new JsonException("Invalid authorization scope.");
    }

    private static JsonElement Required(JsonElement root, string name)
    {
        if (root.ValueKind != JsonValueKind.Object) throw new JsonException("Invalid object.");
        var matches = root.EnumerateObject().Where(p => p.NameEquals(name)).ToArray();
        if (matches.Length != 1) throw new JsonException("Missing or duplicate property.");
        return matches[0].Value;
    }

    private static Guid? ReadGuid(JsonElement value, bool nullable)
    {
        if (nullable && value.ValueKind == JsonValueKind.Null) return null;
        if (value.ValueKind != JsonValueKind.String || !value.TryGetGuid(out var result) || result == Guid.Empty)
            throw new JsonException("Invalid identifier.");
        return result;
    }

    private static bool ValidCode(string value, int max) => !string.IsNullOrWhiteSpace(value) &&
        value.Length <= max && !value.Any(char.IsControl) && value == value.Trim();

    private static IReadOnlyList<string> ReadCodes(JsonElement root, string field)
    {
        var array = Required(root, field);
        if (array.ValueKind != JsonValueKind.Array || array.GetArrayLength() > 2048) throw new JsonException("Invalid code list.");
        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String || item.GetString() is not { } code || !ValidCode(code, 256))
                throw new JsonException("Invalid code.");
            codes.Add(code);
        }
        return Array.AsReadOnly(codes.Order(StringComparer.OrdinalIgnoreCase).ToArray());
    }
}

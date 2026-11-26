using System.Security.Claims;

namespace Itech.Security.AuthGate;

/// <summary>Context checks only, AFTER signature, issuer, type and lifetime validation by JwtBearer.</summary>
public sealed record AuthGateTokenContext(Guid UserId, string Audience, string ClientId, string TokenId)
{
    public static AuthGateTokenContext? Resolve(ClaimsPrincipal principal, AuthGateOidcSettings settings)
    {
        if (principal.Identity?.IsAuthenticated != true) return null;
        var subjects = principal.FindAll("sub").Select(c => c.Value).ToArray();
        var audiences = principal.FindAll("aud").Select(c => c.Value).ToArray();
        var ids = principal.FindAll("jti").Select(c => c.Value).ToArray();
        var clients = principal.FindAll("client_id").Select(c => c.Value).ToArray();
        var parties = principal.FindAll("azp").Select(c => c.Value).ToArray();
        if (subjects.Length != 1 || !Guid.TryParse(subjects[0], out var userId) || userId == Guid.Empty ||
            audiences.Length != 1 || !string.Equals(audiences[0], settings.Audience, StringComparison.Ordinal) ||
            ids.Length != 1 || string.IsNullOrWhiteSpace(ids[0]) || clients.Length > 1 || parties.Length > 1)
            return null;
        var bindings = clients.Concat(parties).ToArray();
        if ((bindings.Length == 0 && !settings.AllowMissingClientBinding) ||
            bindings.Any(c => !string.Equals(c, settings.ClientId, StringComparison.Ordinal))) return null;
        return new(userId, audiences[0], settings.ClientId, ids[0]);
    }
}

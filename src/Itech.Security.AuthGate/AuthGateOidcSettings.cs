using Microsoft.Extensions.Configuration;

namespace Itech.Security.AuthGate;

/// <summary>One dedicated OIDC resource and browser client. No product defaults.</summary>
public sealed record AuthGateOidcSettings(string Issuer, string Audience, string ClientId,
    bool AllowMissingClientBinding = false, int SessionTimeoutSeconds = 5)
{
    public static AuthGateOidcSettings FromConfiguration(IConfiguration section) => new(
        section["Issuer"] ?? string.Empty, section["Audience"] ?? string.Empty,
        section["ClientId"] ?? string.Empty, section.GetValue<bool>("AllowMissingClientBinding"),
        section.GetValue("SessionTimeoutSeconds", 5));

    public void Validate()
    {
        if (!Uri.TryCreate(Issuer, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            uri.AbsolutePath != "/" || uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0 ||
            !string.Equals(Issuer, Issuer.Trim(), StringComparison.Ordinal) ||
            !ValidIdentifier(Audience) || !ValidIdentifier(ClientId) || SessionTimeoutSeconds is < 1 or > 30)
            throw new InvalidOperationException("Invalid AuthGate OIDC resource configuration.");
    }

    private static bool ValidIdentifier(string value) => !string.IsNullOrWhiteSpace(value) &&
        value.Length <= 512 && !value.Any(char.IsWhiteSpace) && !value.Any(char.IsControl);

    internal Uri SessionEndpoint => new(new Uri(Issuer, UriKind.Absolute), "/api/oidc/session/active");
}

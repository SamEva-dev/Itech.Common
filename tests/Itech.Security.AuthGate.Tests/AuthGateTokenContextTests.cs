using System.Security.Claims;
using Itech.Security.AuthGate;

namespace Itech.Security.AuthGate.Tests;

public sealed class AuthGateTokenContextTests
{
    private static readonly AuthGateOidcSettings Settings = new("https://auth.example.invalid/", "api://example", "example-web");
    private static ClaimsPrincipal Principal(string audience = "api://example", string? client = "example-web",
        string? azp = null, bool authenticated = true, bool duplicateAudience = false)
    {
        var claims = new List<Claim> { new("sub", Guid.NewGuid().ToString()), new("aud", audience), new("jti", "token") };
        if (client is not null) claims.Add(new("client_id", client));
        if (azp is not null) claims.Add(new("azp", azp));
        if (duplicateAudience) claims.Add(new("aud", audience));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticated ? "Bearer" : null));
    }

    [Fact] public void AcceptsMatchingContext() => Assert.NotNull(AuthGateTokenContext.Resolve(Principal(), Settings));
    [Fact] public void RejectsWrongAudience() => Assert.Null(AuthGateTokenContext.Resolve(Principal(audience: "api://other"), Settings));
    [Fact] public void RejectsWrongClient() => Assert.Null(AuthGateTokenContext.Resolve(Principal(client: "other"), Settings));
    [Fact] public void RejectsContradictoryBindings() => Assert.Null(AuthGateTokenContext.Resolve(Principal(azp: "other"), Settings));
    [Fact] public void RejectsDuplicateAudience() => Assert.Null(AuthGateTokenContext.Resolve(Principal(duplicateAudience: true), Settings));
    [Fact] public void RejectsUnauthenticatedPrincipal() => Assert.Null(AuthGateTokenContext.Resolve(Principal(authenticated: false), Settings));
    [Fact] public void RejectsMissingBindingByDefault() => Assert.Null(AuthGateTokenContext.Resolve(Principal(client: null), Settings));
    [Fact] public void AllowsExplicitCompatibility() => Assert.NotNull(AuthGateTokenContext.Resolve(Principal(client: null), Settings with { AllowMissingClientBinding = true }));
    [Theory]
    [InlineData("http://auth.example.invalid/")]
    [InlineData("https://auth.example.invalid/path")]
    [InlineData("https://user@auth.example.invalid/")]
    [InlineData("https://auth.example.invalid/?query=yes")]
    public void RejectsUnsafeIssuer(string issuer) => Assert.Throws<InvalidOperationException>(() => (Settings with { Issuer = issuer }).Validate());
}

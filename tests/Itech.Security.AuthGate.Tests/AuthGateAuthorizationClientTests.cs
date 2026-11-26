using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging.Abstractions;

namespace Itech.Security.AuthGate.Tests;

public sealed class AuthGateAuthorizationClientTests
{
    private static readonly AuthGateOidcSettings Settings = new("https://auth.example.invalid/", "api://example", "example-web");
    private static readonly Guid Subject = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid Org = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static AuthenticateResult Ticket(string audience = "api://example", bool withToken = true)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new("sub", Subject.ToString()), new("aud", audience), new("client_id", "example-web"), new("jti", "id")
        }, "Bearer"));
        var props = new AuthenticationProperties();
        if (withToken) props.StoreTokens(new[] { new AuthenticationToken { Name = "access_token", Value = "test-token" } });
        return AuthenticateResult.Success(new AuthenticationTicket(principal, props, "AuthGateOidc"));
    }
    private static JsonObject PermissionBody(int version = 1) => new()
    {
        ["subject"] = Subject.ToString(), ["applicationCode"] = "example", ["organizationId"] = Org.ToString(),
        ["version"] = version, ["permissionsResolved"] = true, ["permissions"] = new JsonArray(JsonValue.Create("read")),
        ["explicitAllowOverrides"] = new JsonArray(), ["contextualScopes"] = new JsonArray()
    };
    private static JsonObject MembershipBody() => new()
    {
        ["subject"] = Subject.ToString(), ["applicationCode"] = "example", ["clientId"] = "example-web",
        ["memberships"] = new JsonArray(new JsonObject { ["organizationId"] = Org.ToString(), ["initialRole"] = "Member" })
    };
    private static AuthGateAuthorizationClient Client(Stub handler) => new(new Factory(handler), NullLogger<AuthGateAuthorizationClient>.Instance);

    [Theory]
    [InlineData(1)] [InlineData(2)]
    public async Task ResolvesExactVersion(int version)
    {
        var handler = new Stub(200, PermissionBody(version).ToJsonString());
        var result = await Client(handler).ResolvePermissionsAsync(Ticket(), Settings, "example", Org, version);
        Assert.Equal(AuthGateAuthorizationState.Resolved, result.State);
        Assert.Equal(version, result.Value!.Version);
        Assert.Equal(new[] { "read" }, result.Value.Permissions.ToArray());
        Assert.Equal($"https://auth.example.invalid/api/oidc/permissions/effective?version={version}&organizationId={Org:D}", handler.Url);
    }
    [Theory]
    [InlineData("subject", "00000000-0000-0000-0000-000000000099")]
    [InlineData("applicationCode", "other")]
    [InlineData("organizationId", "00000000-0000-0000-0000-000000000099")]
    [InlineData("version", "2")]
    [InlineData("permissionsResolved", "true")]
    [InlineData("permissions", "read")]
    public async Task RejectsWrongContextOrType(string key, string value)
    {
        var body = PermissionBody(); body[key] = value;
        var result = await Client(new Stub(200, body.ToJsonString())).ResolvePermissionsAsync(Ticket(), Settings, "example", Org);
        Assert.Equal(AuthGateAuthorizationState.Unavailable, result.State); Assert.Null(result.Value);
    }
    [Fact]
    public async Task RejectsDuplicateFieldsAndUnknownAllows()
    {
        var body = PermissionBody(); body["explicitAllowOverrides"] = new JsonArray(JsonValue.Create("write"));
        foreach (var json in new[] { body.ToJsonString(), PermissionBody().ToJsonString().Insert(1, "\"version\":1,") })
        {
            var result = await Client(new Stub(200, json)).ResolvePermissionsAsync(Ticket(), Settings, "example", Org);
            Assert.Equal(AuthGateAuthorizationState.Unavailable, result.State); Assert.Null(result.Value);
        }
    }
    [Fact]
    public async Task RejectsV2OverridesAndScopes()
    {
        var body = PermissionBody(2); body["contextualScopes"] = new JsonArray(JsonValue.Create("scope"));
        var result = await Client(new Stub(200, body.ToJsonString())).ResolvePermissionsAsync(Ticket(), Settings, "example", Org, 2);
        Assert.Equal(AuthGateAuthorizationState.Unavailable, result.State); Assert.Null(result.Value);
    }
    [Theory]
    [InlineData(401, AuthGateAuthorizationState.Denied)] [InlineData(403, AuthGateAuthorizationState.Denied)]
    [InlineData(409, AuthGateAuthorizationState.Conflict)] [InlineData(404, AuthGateAuthorizationState.Unavailable)]
    [InlineData(302, AuthGateAuthorizationState.Unavailable)] [InlineData(503, AuthGateAuthorizationState.Unavailable)]
    public async Task MapsStatusWithoutGrant(int status, AuthGateAuthorizationState expected)
    {
        var result = await Client(new Stub(status, "{}")).ResolvePermissionsAsync(Ticket(), Settings, "example", Org);
        Assert.Equal(expected, result.State); Assert.Null(result.Value);
    }
    [Fact]
    public async Task RejectsInvalidTicketsWithoutSending()
    {
        var handler = new Stub(200, "{}");
        foreach (var ticket in new[] { AuthenticateResult.NoResult(), Ticket("api://other"), Ticket(withToken: false) })
        {
            var result = await Client(handler).ListMembershipsAsync(ticket, Settings, "example");
            Assert.Equal(AuthGateAuthorizationState.Denied, result.State); Assert.Null(result.Value);
        }
        Assert.Equal(0, handler.Calls);
    }
    [Fact]
    public async Task ListsOnlyBoundMembershipMetadata()
    {
        var handler = new Stub(200, MembershipBody().ToJsonString());
        var result = await Client(handler).ListMembershipsAsync(Ticket(), Settings, "example");
        Assert.Equal(AuthGateAuthorizationState.Resolved, result.State);
        Assert.Equal(new AuthGateMembership(Org, "Member"), Assert.Single(result.Value!.Memberships));
        Assert.Equal("https://auth.example.invalid/api/oidc/memberships", handler.Url);
    }
    [Fact]
    public async Task RejectsForeignClientAndDuplicateOrganization()
    {
        var foreign = MembershipBody(); foreign["clientId"] = "other-web";
        var duplicate = MembershipBody(); var rows = duplicate["memberships"]!.AsArray(); rows.Add(rows[0]!.DeepClone());
        foreach (var body in new[] { foreign, duplicate })
        {
            var result = await Client(new Stub(200, body.ToJsonString())).ListMembershipsAsync(Ticket(), Settings, "example");
            Assert.Equal(AuthGateAuthorizationState.Unavailable, result.State); Assert.Null(result.Value);
        }
    }
    [Fact]
    public async Task RejectsLargeResponse()
    {
        var result = await Client(new Stub(200, new string('x', 65537))).ListMembershipsAsync(Ticket(), Settings, "example");
        Assert.Equal(AuthGateAuthorizationState.Unavailable, result.State); Assert.Null(result.Value);
    }
    private sealed class Factory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }
    private sealed class Stub(int status, string body) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public string? Url { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++; Url = request.RequestUri?.AbsoluteUri;
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("Bearer test-token", request.Headers.Authorization?.ToString());
            return Task.FromResult(new HttpResponseMessage((HttpStatusCode)status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }
}

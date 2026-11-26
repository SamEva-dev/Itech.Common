using System.Net;
using System.Text;
using System.Text.Json;
using Itech.Security.AuthGate;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;

namespace Itech.Security.AuthGate.Tests;

public sealed class AuthGateMachineTokenClientTests
{
    private static readonly AuthGateMachineSettings Settings = new("https://auth.example.invalid/", "issuer-example",
        "service-example", "example.provisioning", "ExampleApi");
    private const string Secret = "test-only-machine-secret";

    [Theory]
    [InlineData("valid", AuthGateMachineTokenState.Acquired)]
    [InlineData("audience", AuthGateMachineTokenState.Unavailable)]
    [InlineData("audiences", AuthGateMachineTokenState.Unavailable)]
    [InlineData("scope", AuthGateMachineTokenState.Unavailable)]
    [InlineData("subject", AuthGateMachineTokenState.Unavailable)]
    [InlineData("party", AuthGateMachineTokenState.Unavailable)]
    [InlineData("issuer", AuthGateMachineTokenState.Unavailable)]
    [InlineData("expired", AuthGateMachineTokenState.Unavailable)]
    [InlineData("future", AuthGateMachineTokenState.Unavailable)]
    [InlineData("algorithm", AuthGateMachineTokenState.Unavailable)]
    [InlineData("duplicate", AuthGateMachineTokenState.Unavailable)]
    [InlineData("duration", AuthGateMachineTokenState.Unavailable)]
    [InlineData("malformed", AuthGateMachineTokenState.Unavailable)]
    public async Task ValidatesMachineResponseContext(string variant, AuthGateMachineTokenState expected)
    {
        var token = Token(variant);
        var factory = new Factory(async (request, ct) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("https://auth.example.invalid/api/m2m/token", request.RequestUri!.AbsoluteUri);
            Assert.Null(request.Headers.Authorization);
            using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
            Assert.Equal(Settings.ClientId, json.RootElement.GetProperty("clientId").GetString());
            Assert.Equal(Secret, json.RootElement.GetProperty("clientSecret").GetString());
            Assert.Equal(Settings.Scope, json.RootElement.GetProperty("scope").GetString());
            Assert.False(json.RootElement.TryGetProperty("audience", out _));
            return Json(HttpStatusCode.OK, JsonSerializer.Serialize(new { accessToken = token, expiresIn = variant == "duration" ? 301 : 120 }));
        });
        var result = await Client(factory).AcquireAsync(Settings, Secret);
        Assert.Equal(expected, result.State);
        Assert.Equal(expected == AuthGateMachineTokenState.Acquired ? token : null, result.AccessToken);
        Assert.DoesNotContain(token, result.ToString()!);
        Assert.Equal(1, factory.Calls);
    }

    [Theory]
    [InlineData(401, AuthGateMachineTokenState.Denied)]
    [InlineData(403, AuthGateMachineTokenState.Denied)]
    [InlineData(302, AuthGateMachineTokenState.Unavailable)]
    [InlineData(503, AuthGateMachineTokenState.Unavailable)]
    [InlineData(429, AuthGateMachineTokenState.Unavailable)]
    public async Task DoesNotRetryOrReturnTokenOnHttpFailure(int status, AuthGateMachineTokenState expected)
    {
        var factory = new Factory((_, _) => Task.FromResult(Json((HttpStatusCode)status, "{}")));
        var result = await Client(factory).AcquireAsync(Settings, Secret);
        Assert.Equal(expected, result.State);
        Assert.Null(result.AccessToken);
        Assert.Equal(1, factory.Calls);
    }

    [Fact]
    public async Task RejectsAmbiguousResponseAndOversizedResponse()
    {
        var factory = new Factory((_, _) => Task.FromResult(Json(HttpStatusCode.OK,
            "{\"accessToken\":\"a\",\"AccessToken\":\"b\",\"expiresIn\":120}")));
        Assert.Equal(AuthGateMachineTokenState.Unavailable, (await Client(factory).AcquireAsync(Settings, Secret)).State);
        factory = new Factory((_, _) => Task.FromResult(Json(HttpStatusCode.OK, new string('x', 33000))));
        Assert.Equal(AuthGateMachineTokenState.Unavailable, (await Client(factory).AcquireAsync(Settings, Secret)).State);
    }

    [Fact]
    public async Task NeverForwardsSecretToTargetAndPreservesIdempotencyKey()
    {
        var token = Token("valid");
        var factory = new Factory(async (request, ct) =>
        {
            if (request.RequestUri!.Host == "auth.example.invalid")
                return Json(HttpStatusCode.OK, JsonSerializer.Serialize(new { accessToken = token, expiresIn = 120 }));
            Assert.Equal("https://api.example.invalid/api/provisioning/organizations", request.RequestUri.AbsoluteUri);
            Assert.Equal("Bearer " + token, request.Headers.Authorization?.ToString());
            Assert.False(request.Headers.Contains("X-Internal-Api-Key"));
            Assert.Equal("operation-123", request.Headers.GetValues("Idempotency-Key").Single());
            Assert.DoesNotContain(Secret, await request.Content!.ReadAsStringAsync(ct));
            return Json(HttpStatusCode.Accepted, "{}");
        });
        using var response = await new AuthGateServiceClient(factory, Client(factory)).SendMachineAsync(
            new(Settings, new Uri("https://api.example.invalid/api/provisioning/organizations")), Secret,
            HttpMethod.Post, new { name = "example" }, "operation-123");
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(2, factory.Calls);
    }

    [Fact]
    public async Task DoesNotCallTargetWhenTokenDenied()
    {
        var factory = new Factory((_, _) => Task.FromResult(Json(HttpStatusCode.Forbidden, "{}")));
        var service = new AuthGateServiceClient(factory, Client(factory));
        var error = await Assert.ThrowsAsync<AuthGateMachineCredentialException>(() => service.SendMachineAsync(
            new(Settings, new Uri("https://api.example.invalid/resource")), Secret, HttpMethod.Get));
        Assert.Equal(AuthGateMachineTokenState.Denied, error.State);
        Assert.Equal(1, factory.Calls);
    }

    [Theory]
    [InlineData("http://api.example.invalid/resource")]
    [InlineData("https://user:pass@api.example.invalid/resource")]
    [InlineData("https://api.example.invalid/resource#fragment")]
    public async Task RejectsUnsafeDestinationBeforeAcquisition(string endpoint)
    {
        var factory = new Factory((_, _) => throw new InvalidOperationException("Unexpected request."));
        await Assert.ThrowsAsync<ArgumentException>(() => new AuthGateServiceClient(factory, Client(factory))
            .SendMachineAsync(new(Settings, new Uri(endpoint)), Secret, HttpMethod.Get));
        Assert.Equal(0, factory.Calls);
    }

    [Fact]
    public async Task SendsScopedKeyOnlyToKnownAuthGateRoute()
    {
        var key = new string('k', 32);
        var factory = new Factory((request, _) =>
        {
            Assert.Equal("https://auth.example.invalid/internal/application-access/ensure", request.RequestUri!.AbsoluteUri);
            Assert.Equal(key, request.Headers.GetValues("X-Internal-Api-Key").Single());
            Assert.Null(request.Headers.Authorization);
            return Task.FromResult(Json(HttpStatusCode.OK, "{}"));
        });
        var service = new AuthGateServiceClient(factory, Client(factory));
        using var response = await service.SendInternalKeyAsync(new Uri(Settings.Authority), "ensure", key, HttpMethod.Post, new { });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Throws<ArgumentException>(() => { _ = service.SendInternalKeyAsync(new Uri(Settings.Authority), "test-identity", key, HttpMethod.Put); });
        Assert.Equal(1, factory.Calls);
    }

    [Fact]
    public async Task CallerCancellationIsPropagated()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var factory = new Factory((_, ct) => Task.FromCanceled<HttpResponseMessage>(ct));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Client(factory).AcquireAsync(Settings, Secret, cts.Token));
    }

    private static AuthGateMachineTokenClient Client(Factory factory) => new(factory, NullLogger<AuthGateMachineTokenClient>.Instance);
    private static HttpResponseMessage Json(HttpStatusCode status, string body) => new(status)
    { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    // Deliberately synthetic signature: acquisition tests claim consistency over trusted transport,
    // not JWT signature verification. The resource server must perform signature validation.
    private static string Token(string variant)
    {
        if (variant == "malformed") return "not-a-jwt";
        var header = JsonSerializer.Serialize(new { alg = variant == "algorithm" ? "none" : "RS256", typ = "JWT" });
        var body = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["iss"] = variant == "issuer" ? "wrong" : Settings.TokenIssuer,
            ["sub"] = variant == "subject" ? "wrong" : Settings.ClientId,
            ["azp"] = variant == "party" ? "wrong" : Settings.ClientId,
            ["scope"] = variant == "scope" ? "wrong" : Settings.Scope,
            ["aud"] = variant == "audiences" ? (object)new[] { Settings.Audience, "Other" } : variant == "audience" ? "Other" : Settings.Audience,
            ["exp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + (variant == "expired" ? -10 : variant == "future" ? 600 : 120),
            ["jti"] = Guid.NewGuid().ToString()
        });
        if (variant == "duplicate") body = body[..^1] + ",\"scope\":\"wrong\"}";
        return Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(header)) + "." +
            Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(body)) + ".c2ln";
    }

    private sealed class Factory(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : IHttpClientFactory
    {
        public int Calls { get; private set; }
        public HttpClient CreateClient(string name) => new(new Handler((request, ct) => { Calls++; return send(request, ct); }));
    }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request, cancellationToken);
    }
}

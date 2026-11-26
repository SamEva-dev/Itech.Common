using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Itech.Security.AuthGate.Tests;

public sealed class AuthGateConnectionLeaseTests
{
    private static readonly AuthGateOidcSettings Settings = new("https://auth.example.invalid/", "api://example", "example-web");
    private static readonly Guid Subject = Guid.Parse("682671de-7d73-4ed8-b5ef-0d4169c6e756");
    private static ClaimsPrincipal Principal(Guid subject, string? authenticationType = "AuthGateOidcBusiness", string tokenId = "test-jti") =>
        new(new ClaimsIdentity(new[] { new Claim("sub", subject.ToString()), new Claim("jti", tokenId) }, authenticationType));
    private static AuthGateConnectionLease Create(StubHandler handler, DateTimeOffset? expires = null) =>
        new(new AuthGateSessionValidator(new Factory(handler), NullLogger<AuthGateSessionValidator>.Instance),
            Settings, Subject, "test-jti", "test-token", expires ?? DateTimeOffset.UtcNow.AddMinutes(5));

    [Theory]
    [InlineData(200, "{\"active\":true}", true)]
    [InlineData(200, "{\"active\":false}", false)]
    [InlineData(401, "{}", false)]
    [InlineData(403, "{}", false)]
    [InlineData(503, "{}", false)]
    [InlineData(200, "invalid", false)]
    public async Task RechecksOriginalBearer(int status, string body, bool expected)
    {
        var handler = new StubHandler((HttpStatusCode)status, body);
        var lease = Create(handler);
        Assert.Equal(expected, await lease.IsActiveAsync(CancellationToken.None));
        Assert.Equal(1, handler.Calls);
        Assert.DoesNotContain("test-token", lease.ToString());
    }

    [Fact]
    public void CaptureRequiresSameAuthenticatedSubject()
    {
        var context = new DefaultHttpContext();
        var principal = Principal(Subject);
        var lease = Create(new StubHandler(HttpStatusCode.OK, "{}"));
        Assert.Null(AuthGateConnectionLease.FromValidatedRequest(context, principal));
        AuthGateConnectionLease.Capture(context, lease);
        Assert.Same(lease, AuthGateConnectionLease.FromValidatedRequest(context, principal));
        Assert.Null(AuthGateConnectionLease.FromValidatedRequest(context, Principal(Guid.NewGuid())));
        Assert.Null(AuthGateConnectionLease.FromValidatedRequest(context, Principal(Subject, tokenId: "another-jti")));
        Assert.Null(AuthGateConnectionLease.FromValidatedRequest(context, Principal(Subject, null)));
        Assert.Null(AuthGateConnectionLease.FromValidatedRequest(null, principal));
        principal.AddIdentity(new ClaimsIdentity(new[] { new Claim("sub", Subject.ToString()) }, "another"));
        Assert.Null(AuthGateConnectionLease.FromValidatedRequest(context, principal));
    }

    [Fact]
    public async Task ExpirationClosesWithoutRemoteCall()
    {
        var handler = new StubHandler(HttpStatusCode.OK, "{}");
        var lease = Create(handler, DateTimeOffset.UtcNow.AddSeconds(-1));
        Assert.False(await lease.IsActiveAsync(CancellationToken.None));
        var invalidated = 0;
        await lease.WatchAsync(() => invalidated++, CancellationToken.None);
        Assert.Equal(1, invalidated);
        Assert.Equal(0, handler.Calls);
    }

    [Theory]
    [InlineData(401)]
    [InlineData(503)]
    public async Task MonitorClosesOnRejectedOrUnavailable(int status)
    {
        var handler = new StubHandler((HttpStatusCode)status, "{}");
        var lease = Create(handler);
        var invalidated = 0;
        await lease.WatchCoreAsync(() => invalidated++, CancellationToken.None, null, () => TimeSpan.Zero);
        Assert.Equal(1, invalidated);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task DisconnectStopsWithoutRemoteCallOrInvalidation()
    {
        var handler = new StubHandler(HttpStatusCode.OK, "{}");
        var lease = Create(handler);
        await lease.WatchAsync(() => throw new InvalidOperationException("unexpected"), CancellationToken.None, () => false);
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task CancellationStopsWithoutInvalidation()
    {
        var handler = new StubHandler(HttpStatusCode.OK, "{}");
        using var cts = new CancellationTokenSource();
        var lease = Create(handler);
        var watch = lease.WatchAsync(() => throw new InvalidOperationException("unexpected"), cts.Token);
        cts.Cancel();
        await watch;
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task ActiveMonitorStopsWhenConnectionCloses()
    {
        var handler = new StubHandler(HttpStatusCode.OK, "{\"active\":true}");
        var lease = Create(handler);
        await lease.WatchCoreAsync(() => throw new InvalidOperationException("unexpected"), CancellationToken.None,
            () => handler.Calls == 0, () => TimeSpan.Zero);
        Assert.Equal(1, handler.Calls);
    }

    private sealed class Factory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }
    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            Assert.Equal("Bearer test-token", request.Headers.Authorization?.ToString());
            Assert.Equal("https://auth.example.invalid/api/oidc/session/active", request.RequestUri?.AbsoluteUri);
            return Task.FromResult(new HttpResponseMessage(status)
            { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }
}

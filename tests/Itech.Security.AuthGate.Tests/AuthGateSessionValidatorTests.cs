using System.Net;
using System.Text;
using Itech.Security.AuthGate;
using Microsoft.Extensions.Logging.Abstractions;

namespace Itech.Security.AuthGate.Tests;

public sealed class AuthGateSessionValidatorTests
{
    private static readonly AuthGateOidcSettings Settings = new("https://auth.example.invalid/", "api://example", "example-web");

    [Theory]
    [InlineData(200, "{\"active\":true}", AuthGateSessionState.Active)]
    [InlineData(200, "{\"active\":false}", AuthGateSessionState.Rejected)]
    [InlineData(200, "{\"active\":false,\"active\":true}", AuthGateSessionState.Unavailable)]
    [InlineData(200, "not-json", AuthGateSessionState.Unavailable)]
    [InlineData(401, "{}", AuthGateSessionState.Rejected)]
    [InlineData(403, "{}", AuthGateSessionState.Rejected)]
    [InlineData(302, "{}", AuthGateSessionState.Unavailable)]
    [InlineData(503, "{}", AuthGateSessionState.Unavailable)]
    public async Task MapsRemoteResponse(int status, string body, AuthGateSessionState expected)
    {
        var handler = new StubHandler((HttpStatusCode)status, body);
        var validator = new AuthGateSessionValidator(new Factory(handler), NullLogger<AuthGateSessionValidator>.Instance);
        Assert.Equal(expected, await validator.ValidateAsync(Settings, "Bearer test-token"));
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task RejectsMissingBearerWithoutRequest()
    {
        var handler = new StubHandler(HttpStatusCode.OK, "{}");
        var validator = new AuthGateSessionValidator(new Factory(handler), NullLogger<AuthGateSessionValidator>.Instance);
        Assert.Equal(AuthGateSessionState.Rejected, await validator.ValidateAsync(Settings, "Basic test"));
        Assert.Equal(0, handler.Calls);
    }

    private sealed class Factory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }
    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("https://auth.example.invalid/api/oidc/session/active", request.RequestUri?.AbsoluteUri);
            Assert.Equal("Bearer test-token", request.Headers.Authorization?.ToString());
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }
}

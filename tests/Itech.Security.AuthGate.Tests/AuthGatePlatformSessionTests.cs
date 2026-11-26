using System.Net;
using System.Text;
using Itech.Security.AuthGate;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Itech.Security.AuthGate.Tests;

public sealed class AuthGatePlatformSessionTests
{
    private const string Subject = "00000000-0000-0000-0000-000000000001";
    private static readonly AuthGateOidcSettings Settings = new("https://auth.example.invalid/", "api://platform", "platform-admin");

    [Theory]
    [InlineData(200, "{\"active\":true,\"subject\":\"00000000-0000-0000-0000-000000000001\"}", AuthGateSessionState.Active)]
    [InlineData(200, "{\"active\":true}", AuthGateSessionState.Unavailable)]
    [InlineData(200, "{\"active\":true,\"subject\":\"00000000-0000-0000-0000-000000000002\"}", AuthGateSessionState.Unavailable)]
    [InlineData(200, "{\"active\":true,\"subject\":null}", AuthGateSessionState.Unavailable)]
    [InlineData(200, "{\"active\":true,\"subject\":\"00000000-0000-0000-0000-000000000001\",\"Subject\":\"00000000-0000-0000-0000-000000000002\"}", AuthGateSessionState.Unavailable)]
    [InlineData(200, "{\"active\":true,\"Active\":false,\"subject\":\"00000000-0000-0000-0000-000000000001\"}", AuthGateSessionState.Unavailable)]
    [InlineData(200, "{\"active\":false}", AuthGateSessionState.Rejected)]
    [InlineData(401, "{}", AuthGateSessionState.Rejected)]
    [InlineData(403, "{}", AuthGateSessionState.Rejected)]
    [InlineData(302, "{}", AuthGateSessionState.Unavailable)]
    [InlineData(503, "{}", AuthGateSessionState.Unavailable)]
    public async Task PlatformResponseMustProveSameSubject(int status, string body, AuthGateSessionState expected)
    {
        using var handler = new StubHandler((HttpStatusCode)status, body);
        var validator = new AuthGateSessionValidator(new Factory(handler), NullLogger<AuthGateSessionValidator>.Instance);
        Assert.Equal(expected, await validator.ValidatePlatformAsync(Settings, "Bearer test-token", Guid.Parse(Subject)));
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task RejectsMissingSubjectOrRelaxedClientWithoutRequest()
    {
        using var handler = new StubHandler(HttpStatusCode.OK, "{}");
        var validator = new AuthGateSessionValidator(new Factory(handler), NullLogger<AuthGateSessionValidator>.Instance);
        Assert.Equal(AuthGateSessionState.Rejected, await validator.ValidatePlatformAsync(Settings, "Bearer test-token", Guid.Empty));
        Assert.Equal(AuthGateSessionState.Rejected, await validator.ValidatePlatformAsync(Settings with { AllowMissingClientBinding = true }, "Bearer test-token", Guid.Parse(Subject)));
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public void PlatformRegistrationCannotRelaxClientBinding()
    {
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() => services.AddAuthentication()
            .AddAuthGatePlatformOidc("Platform", Settings with { AllowMissingClientBinding = true }));
    }

    [Fact]
    public void PlatformSchemeRetainsStrictJwtParameters()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthentication().AddAuthGatePlatformOidc("Platform", Settings);
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get("Platform");
        Assert.Equal(Settings.Audience, options.TokenValidationParameters.ValidAudience);
        Assert.Equal(Settings.Issuer, options.TokenValidationParameters.ValidIssuer);
        Assert.False(options.TokenValidationParameters.IgnoreTrailingSlashWhenValidatingAudience);
        Assert.True(options.RequireHttpsMetadata);
        Assert.True(options.TokenValidationParameters.ValidateLifetime);
        Assert.Contains("at+jwt", options.TokenValidationParameters.ValidTypes);
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
            Assert.Equal("https://auth.example.invalid/api/oidc/platform/session/validate", request.RequestUri?.AbsoluteUri);
            Assert.Equal("Bearer test-token", request.Headers.Authorization?.ToString());
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }
}

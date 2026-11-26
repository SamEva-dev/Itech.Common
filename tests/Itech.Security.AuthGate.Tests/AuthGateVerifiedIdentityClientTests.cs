using System.Net;
using System.Text;
using Itech.Security.AuthGate;

namespace Itech.Security.AuthGate.Tests;

public sealed class AuthGateVerifiedIdentityClientTests
{
    private static readonly Uri Authority = new("https://auth.example.invalid/");
    private static readonly Guid Subject = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const string Audience = "api://example";
    private const string Valid = "{\"subject\":\"11111111-1111-1111-1111-111111111111\",\"audience\":\"api://example\",\"email\":\"user@example.invalid\"}";

    [Theory]
    [InlineData(200, Valid, AuthGateVerifiedIdentityState.Resolved)]
    [InlineData(401, Valid, AuthGateVerifiedIdentityState.Denied)]
    [InlineData(403, Valid, AuthGateVerifiedIdentityState.Denied)]
    [InlineData(302, Valid, AuthGateVerifiedIdentityState.Unavailable)]
    [InlineData(503, Valid, AuthGateVerifiedIdentityState.Unavailable)]
    [InlineData(200, "{}", AuthGateVerifiedIdentityState.Unavailable)]
    [InlineData(200, "[]", AuthGateVerifiedIdentityState.Unavailable)]
    [InlineData(200, "{", AuthGateVerifiedIdentityState.Unavailable)]
    [InlineData(200, "{\"subject\":\"22222222-2222-2222-2222-222222222222\",\"audience\":\"api://example\",\"email\":\"user@example.invalid\"}", AuthGateVerifiedIdentityState.Denied)]
    [InlineData(200, "{\"subject\":\"11111111-1111-1111-1111-111111111111\",\"audience\":\"api://other\",\"email\":\"user@example.invalid\"}", AuthGateVerifiedIdentityState.Denied)]
    [InlineData(200, "{\"subject\":\"11111111-1111-1111-1111-111111111111\",\"audience\":\"api://example\",\"email\":\"user@example.invalid\",\"Email\":\"other@example.invalid\"}", AuthGateVerifiedIdentityState.Unavailable)]
    [InlineData(200, "{\"subject\":\"11111111-1111-1111-1111-111111111111\",\"audience\":\"api://example\",\"email\":123}", AuthGateVerifiedIdentityState.Unavailable)]
    public async Task ValidatesResponse(int status, string body, AuthGateVerifiedIdentityState expected)
    {
        var factory = new Factory((request, _) =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal(new Uri(Authority, "api/oidc/registration/verified-identity"), request.RequestUri);
            Assert.Equal("test-only-bearer", request.Headers.Authorization?.Parameter);
            Assert.False(request.Headers.Contains("X-Internal-Api-Key"));
            return Task.FromResult(new HttpResponseMessage((HttpStatusCode)status)
            { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        });
        var result = await AuthGateVerifiedIdentityClient.ResolveAsync(factory, Authority,
            "test-only-bearer", Subject, Audience);
        Assert.Equal(expected, result.State);
        Assert.Equal(expected == AuthGateVerifiedIdentityState.Resolved ? "user@example.invalid" : null, result.Email);
        Assert.Equal(1, factory.Calls);
    }

    [Fact]
    public async Task RejectsInvalidAuthorityWithoutSending()
    {
        var factory = new Factory((_, _) => throw new InvalidOperationException("Must not send"));
        var result = await AuthGateVerifiedIdentityClient.ResolveAsync(factory, new Uri("http://auth.example.invalid/"),
            "test-only-bearer", Subject, Audience);
        Assert.Equal(AuthGateVerifiedIdentityState.Unavailable, result.State);
        Assert.Null(result.Email);
        Assert.Equal(0, factory.Calls);
    }

    [Fact]
    public async Task RejectsOversizedResponse()
    {
        var factory = new Factory((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        { Content = new StringContent(new string('x', 16385), Encoding.UTF8, "application/json") }));
        var result = await AuthGateVerifiedIdentityClient.ResolveAsync(factory, Authority, "test-only-bearer", Subject, Audience);
        Assert.Equal(AuthGateVerifiedIdentityState.Unavailable, result.State);
        Assert.Null(result.Email);
        Assert.Equal(1, factory.Calls);
    }

    [Fact]
    public async Task PropagatesCallerCancellation()
    {
        var factory = new Factory((_, _) => throw new InvalidOperationException("Must not send"));
        using var cts = new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => AuthGateVerifiedIdentityClient.ResolveAsync(
            factory, Authority, "test-only-bearer", Subject, Audience, cts.Token));
        Assert.Equal(0, factory.Calls);
    }

    private sealed class Factory(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : IHttpClientFactory
    {
        public int Calls { get; private set; }
        public HttpClient CreateClient(string name)
        {
            Assert.Equal(AuthGateRegistrationTransport.ClientName, name);
            Calls++;
            return new HttpClient(new Handler(send));
        }
    }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => send(request, cancellationToken);
    }
}

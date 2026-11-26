using System.Net;
using System.Text;
using Itech.Security.AuthGate;

namespace Itech.Security.AuthGate.Tests;

public sealed class AuthGateRegistrationTransportTests
{
    private static readonly Uri Authority = new("https://auth.example.invalid/");
    private const string Token = "test-only-bearer";
    private const string Key = "test-only-internal-key-32-characters";

    [Theory]
    [InlineData("api/oidc/registration/context", "GET", false, false)]
    [InlineData("api/oidc/registration/verified-identity", "GET", false, false)]
    [InlineData("api/oidc/registration/organization-enroll", "POST", true, false)]
    [InlineData("api/oidc/registration/self-enroll", "POST", false, false)]
    [InlineData("internal/application-access/ensure", "POST", true, true)]
    public async Task UsesOnlyExpectedDestinationAndCredential(string operation, string method, bool hasBody, bool useKey)
    {
        var factory = new Factory((request, _) =>
        {
            Assert.Equal(new Uri(Authority, operation), request.RequestUri);
            Assert.Equal(method, request.Method.Method);
            Assert.Equal(hasBody, request.Content is not null);
            Assert.Equal(useKey, request.Headers.Contains("X-Internal-Api-Key"));
            if (useKey) Assert.Equal(Key, request.Headers.GetValues("X-Internal-Api-Key").Single());
            Assert.Equal(useKey ? null : Token, request.Headers.Authorization?.Parameter);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict)
            { Content = new StringContent("{}", Encoding.UTF8, "application/json") });
        });
        using var response = await AuthGateRegistrationTransport.SendAsync(factory, Authority, operation,
            useKey ? null : Token, useKey ? Key : null, hasBody ? new { name = "test" } : null);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("{}", await response.Content.ReadAsStringAsync());
        Assert.Equal(1, factory.Calls);
    }

    [Theory]
    [InlineData("http://auth.example.invalid/", "api/oidc/registration/context", false)]
    [InlineData("https://auth.example.invalid/path/", "api/oidc/registration/context", false)]
    [InlineData("https://auth.example.invalid/?q=1", "api/oidc/registration/context", false)]
    [InlineData("https://auth.example.invalid/", "https://other.example.invalid/", false)]
    [InlineData("https://auth.example.invalid/", "internal/application-access/test-identity", true)]
    [InlineData("https://auth.example.invalid/", "api/oidc/registration/context", true)]
    public async Task RejectsInvalidBindingsBeforeSending(string authority, string operation, bool bothCredentials)
    {
        var factory = new Factory((_, _) => throw new InvalidOperationException("Must not send"));
        await Assert.ThrowsAsync<HttpRequestException>(() => AuthGateRegistrationTransport.SendAsync(factory,
            new Uri(authority), operation, Token, bothCredentials ? Key : null, null));
        Assert.Equal(0, factory.Calls);
    }

    [Fact]
    public async Task RejectsOversizedResponse()
    {
        var factory = new Factory((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        { Content = new StringContent(new string('x', 16385)) }));
        await Assert.ThrowsAsync<AuthGateTransportException>(() => AuthGateRegistrationTransport.SendAsync(factory,
            Authority, "api/oidc/registration/context", Token, null, null));
    }

    [Fact]
    public async Task DoesNotSendWhenCallerAlreadyCancelled()
    {
        var factory = new Factory((_, _) => throw new InvalidOperationException("Must not send"));
        using var cts = new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => AuthGateRegistrationTransport.SendAsync(factory,
            Authority, "api/oidc/registration/self-enroll", Token, null, null, cts.Token));
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

using System.Net;
using System.Net.Http.Headers;
using Itech.Security.AuthGate;
using Microsoft.Extensions.DependencyInjection;

namespace Itech.Security.AuthGate.Tests;

public sealed class AuthGateTransportResilienceTests
{
    private const string Session = "Itech.Security.AuthGate.Session";
    private const string Machine = "Itech.Security.AuthGate.MachineToken";
    private const string Service = "Itech.Security.AuthGate.Service";
    private static readonly Uri First = new("https://first.example.invalid/path");
    private static readonly Uri Second = new("https://second.example.invalid/path");

    [Theory]
    [InlineData(502, 2)]
    [InlineData(503, 2)]
    [InlineData(504, 2)]
    [InlineData(401, 1)]
    [InlineData(403, 1)]
    [InlineData(429, 1)]
    [InlineData(500, 1)]
    public async Task OnlySelectedReadResponsesAreRetried(int status, int expectedCalls)
    {
        var calls = 0;
        var ids = new List<string>();
        using var provider = Build((request, _) =>
        {
            ids.Add(request.Headers.GetValues("X-Correlation-Id").Single());
            return Task.FromResult(new HttpResponseMessage(++calls == 1 ? (HttpStatusCode)status : HttpStatusCode.OK));
        });
        using var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(Session);
        using var response = await client.GetAsync(First);
        Assert.Equal(expectedCalls, calls);
        Assert.Single(ids.Distinct());
        Assert.True(Guid.TryParse(ids[0], out _));
        Assert.Equal(expectedCalls == 2 ? HttpStatusCode.OK : (HttpStatusCode)status, response.StatusCode);
    }

    [Theory]
    [InlineData(Machine, "POST")]
    [InlineData(Service, "POST")]
    [InlineData(Service, "GET")]
    [InlineData(Session, "POST")]
    public async Task NeverRetriesWritesOrUnapprovedClients(string purpose, string method)
    {
        var calls = 0;
        using var provider = Build((_, _) => { calls++; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)); });
        using var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(purpose);
        using var request = new HttpRequestMessage(new HttpMethod(method), First);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task RespectsRetryAfter()
    {
        var calls = 0;
        using var provider = Build((_, _) =>
        {
            calls++;
            var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(5));
            return Task.FromResult(response);
        });
        using var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(Session);
        using var response = await client.GetAsync(First);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task CircuitIsSharedByClientInstancesButIsolatedByOriginAndPurpose()
    {
        var clock = new ManualClock();
        var fail = true;
        var calls = 0;
        using var provider = Build((request, _) =>
        {
            calls++;
            return Task.FromResult(new HttpResponseMessage(fail && request.RequestUri!.Host == First.Host ? HttpStatusCode.InternalServerError : HttpStatusCode.OK));
        }, clock);
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        using var client = factory.CreateClient(Session);
        using var first = await client.GetAsync(First);
        using var second = await client.GetAsync(First);
        using var another = factory.CreateClient(Session);
        var error = await Assert.ThrowsAsync<AuthGateTransportException>(() => another.GetAsync(First));
        Assert.Equal(AuthGateTransportError.CircuitOpen, error.Error);
        Assert.Equal(2, calls);
        using var otherOrigin = await another.GetAsync(Second);
        using var otherPurpose = factory.CreateClient(Service);
        using var otherResult = await otherPurpose.GetAsync(First);
        Assert.Equal(4, calls);
        clock.Advance(TimeSpan.FromSeconds(31));
        fail = false;
        using var probe = await another.GetAsync(First);
        using var recovered = await client.GetAsync(First);
        Assert.Equal(HttpStatusCode.OK, recovered.StatusCode);
        Assert.Equal(6, calls);
    }

    [Fact]
    public async Task HalfOpenAdmitsOnlyOneProbe()
    {
        var clock = new ManualClock();
        var probeStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseProbe = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        using var provider = Build(async (_, ct) =>
        {
            if (Interlocked.Increment(ref calls) <= 2) return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            probeStarted.TrySetResult(true);
            await releaseProbe.Task.WaitAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }, clock);
        using var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(Session);
        using var first = await client.GetAsync(First);
        using var second = await client.GetAsync(First);
        clock.Advance(TimeSpan.FromSeconds(31));
        var probe = client.GetAsync(First);
        try
        {
            await probeStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
            var error = await Assert.ThrowsAsync<AuthGateTransportException>(() => client.GetAsync(First));
            Assert.Equal(AuthGateTransportError.CircuitOpen, error.Error);
            Assert.Equal(3, calls);
        }
        finally { releaseProbe.TrySetResult(true); using var completed = await probe; }
    }

    [Fact]
    public async Task TransportExceptionDoesNotExposeInnerMessage()
    {
        using var provider = Build((_, _) => throw new HttpRequestException("private URL or credential"));
        using var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(Service);
        var error = await Assert.ThrowsAsync<AuthGateTransportException>(() => client.GetAsync(First));
        Assert.Equal(AuthGateTransportError.TransportUnavailable, error.Error);
        Assert.Null(error.InnerException);
        Assert.DoesNotContain("private", error.ToString());
    }

    [Fact]
    public async Task CallerCancellationIsNotConvertedToServiceFailure()
    {
        using var provider = Build(async (_, ct) => { await Task.Delay(Timeout.Infinite, ct); return new HttpResponseMessage(HttpStatusCode.OK); });
        using var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(Session);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetAsync(First, cancellation.Token));
    }

    private static ServiceProvider Build(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send, ManualClock? clock = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new AuthGateResilienceSettings(2, 30));
        if (clock is not null) services.AddSingleton<TimeProvider>(clock);
        services.AddAuthentication().AddAuthGateOidc("test", new AuthGateOidcSettings("https://auth.example.invalid/", "api://example", "example-web"));
        foreach (var name in new[] { Session, Machine, Service })
            services.AddHttpClient(name).ConfigurePrimaryHttpMessageHandler(() => new Stub(send));
        return services.BuildServiceProvider();
    }

    private sealed class Stub(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request, cancellationToken);
    }

    private sealed class ManualClock : TimeProvider
    {
        private long ticks;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => Interlocked.Read(ref ticks);
        public void Advance(TimeSpan span) => Interlocked.Add(ref ticks, span.Ticks);
    }
}

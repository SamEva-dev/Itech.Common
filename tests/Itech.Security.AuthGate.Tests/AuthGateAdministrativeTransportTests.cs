using System.Net;
using Itech.Security.AuthGate;
using Microsoft.Extensions.DependencyInjection;

namespace Itech.Security.AuthGate.Tests;

public sealed class AuthGateAdministrativeTransportTests
{
    [Theory]
    [InlineData("https://admin.example.invalid/api/users", true)]
    [InlineData("https://admin.example.invalid:443/api/users", true)]
    [InlineData("https://admin.example.invalid:444/api/users", false)]
    [InlineData("https://admin.example.invalid.evil.invalid/api/users", false)]
    [InlineData("http://admin.example.invalid/api/users", false)]
    [InlineData("https://user@admin.example.invalid/api/users", false)]
    [InlineData("https://admin.example.invalid/api/users#fragment", false)]
    public async Task OnlyConfiguredOriginsReachPrimaryHandler(string url, bool accepted)
    {
        var primary = new RecordingHandler();
        using var provider = Build(primary);
        using var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("admin-test");
        if (accepted)
        {
            using var response = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        }
        else await Assert.ThrowsAsync<AuthGateTransportException>(() => client.GetAsync(url));
        Assert.Equal(accepted ? 1 : 0, primary.Calls);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task NoAutomaticAdministrativeRetries(string method)
    {
        var primary = new RecordingHandler();
        using var provider = Build(primary);
        using var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("admin-test");
        using var request = new HttpRequestMessage(new HttpMethod(method), "https://admin.example.invalid/api/users");
        request.Headers.Add("Idempotency-Key", "operation-1");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(1, primary.Calls);
        Assert.Equal("operation-1", primary.IdempotencyKey);
    }

    [Theory]
    [InlineData("http://admin.example.invalid")]
    [InlineData("https://admin.example.invalid/api")]
    [InlineData("https://admin.example.invalid/?a=1")]
    [InlineData("https://user@admin.example.invalid")]
    public void InvalidOriginIsRejectedDuringRegistration(string origin)
    {
        var builder = new ServiceCollection().AddHttpClient("invalid");
        Assert.Throws<InvalidOperationException>(() => builder.WithAuthGateAdministrativeTransport([origin]));
    }

    [Fact]
    public async Task ExplicitHostOverrideIsRejected()
    {
        var primary = new RecordingHandler();
        using var provider = Build(primary);
        using var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("admin-test");
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://admin.example.invalid/api/users");
        request.Headers.Host = "other.example.invalid";
        await Assert.ThrowsAsync<AuthGateTransportException>(() => client.SendAsync(request));
        Assert.Equal(0, primary.Calls);
    }

    private static ServiceProvider Build(RecordingHandler primary)
    {
        var services = new ServiceCollection();
        services.AddHttpClient("admin-test").WithAuthGateAdministrativeTransport(["https://admin.example.invalid/"])
            .ConfigurePrimaryHttpMessageHandler(() => primary);
        return services.BuildServiceProvider();
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public string? IdempotencyKey { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            if (request.Headers.TryGetValues("Idempotency-Key", out var keys)) IdempotencyKey = keys.Single();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        }
    }
}

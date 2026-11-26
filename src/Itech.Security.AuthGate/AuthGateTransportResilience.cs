using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Itech.Security.AuthGate;

/// <summary>Server-owned policy. No automatic replay of requests that can mutate state.</summary>
public sealed record AuthGateResilienceSettings(int FailureThreshold = 5, int BreakSeconds = 30,
    bool RetryReadOnly = true)
{
    internal void Validate()
    {
        if (FailureThreshold is < 2 or > 100 || BreakSeconds is < 1 or > 300)
            throw new InvalidOperationException("Invalid AuthGate resilience configuration.");
    }
}

public enum AuthGateTransportError { CircuitOpen, TransportUnavailable, Timeout, CapacityExceeded }

/// <summary>Safe to map to an application error; deliberately contains no URL, headers or inner exception.</summary>
public sealed class AuthGateTransportException(AuthGateTransportError error)
    : HttpRequestException("AuthGate transport unavailable.")
{
    public AuthGateTransportError Error { get; } = error;
}

// State is shared by pooled HttpClient handlers, isolated by client purpose and exact HTTPS origin.
internal sealed class AuthGateCircuitRegistry(AuthGateResilienceSettings settings, TimeProvider clock)
{
    private const int MaxDestinations = 128;
    private readonly Dictionary<(string, string), Circuit> circuits = new();
    internal Circuit Get(string purpose, Uri destination)
    {
        settings.Validate();
        var key = (purpose, destination.GetLeftPart(UriPartial.Authority));
        lock (circuits)
        {
            if (circuits.TryGetValue(key, out var existing)) return existing;
            if (circuits.Count >= MaxDestinations) throw new AuthGateTransportException(AuthGateTransportError.CapacityExceeded);
            return circuits[key] = new Circuit(settings, clock);
        }
    }

    internal sealed class Circuit(AuthGateResilienceSettings settings, TimeProvider clock)
    {
        private readonly object gate = new();
        private int failures;
        private long generation;
        private long? openedAt;
        private bool probeRunning;
        internal readonly record struct Lease(long Generation, bool Probe);

        internal Lease Enter()
        {
            lock (gate)
            {
                if (openedAt is null) return new(generation, false);
                if (probeRunning || clock.GetElapsedTime(openedAt.Value) < TimeSpan.FromSeconds(settings.BreakSeconds))
                    throw new AuthGateTransportException(AuthGateTransportError.CircuitOpen);
                probeRunning = true;
                return new(generation, true);
            }
        }

        // null = cancelled or unclassified; must release a half-open probe but not count as success/failure.
        internal void Complete(Lease lease, bool? failure)
        {
            lock (gate)
            {
                if (lease.Generation != generation) return; // stale in-flight request
                if (lease.Probe) probeRunning = false;
                if (!failure.HasValue) return;
                if (!failure.Value)
                {
                    failures = 0;
                    if (lease.Probe) { openedAt = null; generation++; }
                    return;
                }
                if (lease.Probe || ++failures >= settings.FailureThreshold)
                {
                    openedAt = clock.GetTimestamp();
                    generation++;
                    probeRunning = false;
                }
            }
        }
    }
}

internal sealed class AuthGateTransportHandler(string purpose, bool allowReadRetry,
    AuthGateCircuitRegistry circuits, AuthGateResilienceSettings settings, ILogger<AuthGateTransportHandler> logger)
    : DelegatingHandler
{
    private static readonly Meter Meter = new("Itech.Security.AuthGate", "1.0.0");
    private static readonly Counter<long> Requests = Meter.CreateCounter<long>("authgate.transport.requests");
    private static readonly Counter<long> Retries = Meter.CreateCounter<long>("authgate.transport.retries");
    private static readonly Histogram<double> Duration = Meter.CreateHistogram<double>("authgate.transport.duration", "ms");

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var start = Stopwatch.GetTimestamp();
        var correlation = Guid.NewGuid().ToString("D");
        var outcome = "unavailable";
        AuthGateCircuitRegistry.Circuit? circuit = null;
        AuthGateCircuitRegistry.Circuit.Lease? lease = null;
        bool? failed = null;
        try
        {
            ct.ThrowIfCancellationRequested();
            if (request.RequestUri is not { IsAbsoluteUri: true } destination || !AuthGateServiceUri.IsHttps(destination))
                throw new AuthGateTransportException(AuthGateTransportError.TransportUnavailable);
            circuit = circuits.Get(purpose, destination);
            lease = circuit.Enter();
            request.Headers.Remove("X-Correlation-Id");
            request.Headers.Add("X-Correlation-Id", correlation);
            var response = await base.SendAsync(request, ct).ConfigureAwait(false);
            if (allowReadRetry && settings.RetryReadOnly && request.Method == HttpMethod.Get && request.Content is null &&
                (response.StatusCode is HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout) &&
                response.Headers.RetryAfter is null)
            {
                response.Dispose();
                await Task.Delay(TimeSpan.FromMilliseconds(Random.Shared.Next(100, 251)), ct).ConfigureAwait(false);
                using var retry = new HttpRequestMessage(HttpMethod.Get, request.RequestUri)
                { Version = request.Version, VersionPolicy = request.VersionPolicy };
                foreach (var header in request.Headers) retry.Headers.TryAddWithoutValidation(header.Key, header.Value);
                foreach (var option in request.Options) retry.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);
                Retries.Add(1, new KeyValuePair<string, object?>("purpose", purpose));
                response = await base.SendAsync(retry, ct).ConfigureAwait(false);
                response.RequestMessage = request;
            }
            var status = (int)response.StatusCode;
            failed = (status is 408 or 429) || status >= 500;
            outcome = failed.Value ? "remote_failure" : status is 401 or 403 ? "denied" : status < 300 ? "success" : "other_http";
            return response;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            outcome = "cancelled";
            throw;
        }
        catch (AuthGateTransportException ex)
        {
            outcome = ex.Error == AuthGateTransportError.CircuitOpen ? "circuit_open" : "unavailable";
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or OperationCanceledException)
        {
            failed = true;
            outcome = "transport_failure";
            throw new AuthGateTransportException(AuthGateTransportError.TransportUnavailable);
        }
        finally
        {
            if (lease.HasValue) circuit!.Complete(lease.Value, failed);
            var tags = new TagList { { "purpose", purpose }, { "outcome", outcome } };
            Requests.Add(1, tags);
            Duration.Record(Stopwatch.GetElapsedTime(start).TotalMilliseconds, tags);
            if (outcome is "remote_failure" or "transport_failure" or "unavailable")
                logger.LogWarning("AuthGate transport {Purpose}: {Outcome}; correlation {CorrelationId}.", purpose, outcome, correlation);
        }
    }
}

internal static class AuthGateTransportRegistration
{
    internal static IHttpClientBuilder WithAuthGateTransport(this IHttpClientBuilder builder, string purpose, bool retryReads = false)
    {
        builder.Services.TryAddSingleton(new AuthGateResilienceSettings());
        builder.Services.TryAddSingleton<TimeProvider>(TimeProvider.System);
        builder.Services.TryAddSingleton<AuthGateCircuitRegistry>(sp => new AuthGateCircuitRegistry(
            sp.GetRequiredService<AuthGateResilienceSettings>(), sp.GetRequiredService<TimeProvider>()));
        return builder.RemoveAllLoggers().AddHttpMessageHandler(sp =>
            new AuthGateTransportHandler(purpose, retryReads, sp.GetRequiredService<AuthGateCircuitRegistry>(),
                sp.GetRequiredService<AuthGateResilienceSettings>(), sp.GetRequiredService<ILogger<AuthGateTransportHandler>>()));
    }
}

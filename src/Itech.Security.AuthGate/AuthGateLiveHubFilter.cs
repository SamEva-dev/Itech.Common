using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Itech.Security.AuthGate;

/// <summary>Opt-in filter for product hubs whose middleware converts a verified OIDC identity
/// to a business principal. Legacy sessions are deliberately unaffected.</summary>
public sealed class AuthGateLiveHubFilter(ILogger<AuthGateLiveHubFilter> logger) : IHubFilter
{
    private static readonly object LeaseKey = new();
    private static readonly object WatchKey = new();
    private sealed record Monitor(CancellationTokenSource Stop, Task Task);
    private static bool IsOidc(HubCallerContext context) =>
        context.User?.Identities.Any(i => i.AuthenticationType == "AuthGateOidcBusiness") == true ||
        AuthGateConnectionLease.FromValidatedRequest(context.GetHttpContext(), context.User) is not null;

    public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        var caller = context.Context;
        if (!IsOidc(caller)) { await next(context); return; }
        var lease = AuthGateConnectionLease.FromValidatedRequest(caller.GetHttpContext(), caller.User);
        if (lease is null || !await lease.IsActiveAsync(caller.ConnectionAborted))
        { caller.Abort(); throw new HubException("realtime.session_invalid"); }
        caller.Items[LeaseKey] = lease;
        // Do not retain a scoped service provider or the request ExecutionContext in the monitor.
        var stop = CancellationTokenSource.CreateLinkedTokenSource(caller.ConnectionAborted);
        var token = stop.Token;
        Task watch;
        using (ExecutionContext.SuppressFlow())
            watch = Task.Run(() => lease.WatchAsync(() =>
            {
                logger.LogInformation("Closing SignalR connection after AuthGate lease invalidation.");
                caller.Abort();
            }, token));
        caller.Items[WatchKey] = new Monitor(stop, watch);
        try { await next(context); }
        catch
        {
            caller.Abort();
            await StopAsync(caller);
            throw;
        }
    }

    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocation,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var caller = invocation.Context;
        if (caller.Items.TryGetValue(LeaseKey, out var item) && item is AuthGateConnectionLease lease)
        {
            if (!await lease.IsActiveAsync(caller.ConnectionAborted))
            { caller.Abort(); throw new HubException("realtime.session_invalid"); }
        }
        else if (IsOidc(caller))
        { caller.Abort(); throw new HubException("realtime.session_invalid"); }
        return await next(invocation);
    }

    public async Task OnDisconnectedAsync(HubLifetimeContext context, Exception? exception,
        Func<HubLifetimeContext, Exception?, Task> next)
    {
        try { await StopAsync(context.Context); }
        finally { await next(context, exception); }
    }

    private static async Task StopAsync(HubCallerContext caller)
    {
        if (caller.Items.TryGetValue(WatchKey, out var item) && item is Monitor monitor)
        {
            caller.Items.Remove(WatchKey);
            try { monitor.Stop.Cancel(); await monitor.Task; }
            finally { monitor.Stop.Dispose(); }
        }
        caller.Items.Remove(LeaseKey);
    }
}

public static class AuthGateLiveHubExtensions
{
    public static IServiceCollection AddAuthGateLiveHubs(this IServiceCollection services)
    {
        services.AddSingleton<AuthGateLiveHubFilter>();
        services.Configure<HubOptions>(o => o.AddFilter<AuthGateLiveHubFilter>());
        return services;
    }
}

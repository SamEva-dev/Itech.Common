using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Itech.Security.AuthGate;

/// <summary>In-memory proof captured only after JwtBearer and live session validation.
/// Never serialize, log or persist the original bearer. No HttpContext is retained.</summary>
public sealed class AuthGateConnectionLease
{
    private static readonly object ItemKey = new();
    private readonly AuthGateSessionValidator _validator;
    private readonly AuthGateOidcSettings _settings;
    private readonly string _authorization;
    private readonly bool _platform;
    public Guid Subject { get; }
    private readonly string _tokenId;
    public DateTimeOffset ExpiresAt { get; }

    internal AuthGateConnectionLease(AuthGateSessionValidator validator, AuthGateOidcSettings settings,
        Guid subject, string tokenId, string token, DateTimeOffset expiresAt, bool platform = false)
    {
        _validator = validator; _settings = settings; Subject = subject;
        _tokenId = tokenId; _authorization = "Bearer " + token; ExpiresAt = expiresAt; _platform = platform;
    }

    internal static void Capture(HttpContext context, AuthGateConnectionLease lease) => context.Items[ItemKey] = lease;

    public static AuthGateConnectionLease? FromValidatedRequest(HttpContext? context, ClaimsPrincipal? principal)
    {
        if (context is null || principal?.Identity?.IsAuthenticated != true ||
            !context.Items.TryGetValue(ItemKey, out var item) || item is not AuthGateConnectionLease lease)
            return null;
        var subjects = principal.FindAll("sub").ToArray();
        var ids = principal.FindAll("jti").ToArray();
        return subjects.Length == 1 && Guid.TryParse(subjects[0].Value, out var subject) && subject == lease.Subject &&
            ids.Length == 1 && string.Equals(ids[0].Value, lease._tokenId, StringComparison.Ordinal)
            ? lease : null;
    }

    public async Task<bool> IsActiveAsync(CancellationToken ct)
    {
        if (ExpiresAt <= DateTimeOffset.UtcNow) return false;
        var result = _platform
            ? await _validator.ValidatePlatformAsync(_settings, _authorization, Subject, ct)
            : await _validator.ValidateAsync(_settings, _authorization, ct);
        return result == AuthGateSessionState.Active && ExpiresAt > DateTimeOffset.UtcNow;
    }

    // Every replica checks its own connections; no affinity to the instance issuing the JWT.
    // Fail closed on dependency failure. The lifetime token stops the loop on normal disconnect.
    public Task WatchAsync(Action invalidate, CancellationToken lifetime, Func<bool>? stillConnected = null) =>
        WatchCoreAsync(invalidate, lifetime, stillConnected, () => TimeSpan.FromSeconds(Random.Shared.Next(10, 16)));

    internal async Task WatchCoreAsync(Action invalidate, CancellationToken lifetime,
        Func<bool>? stillConnected, Func<TimeSpan> interval)
    {
        try
        {
            while (!lifetime.IsCancellationRequested)
            {
                if (stillConnected?.Invoke() == false) return;
                var remaining = ExpiresAt - DateTimeOffset.UtcNow;
                if (remaining <= TimeSpan.Zero) break;
                var delay = interval();
                await Task.Delay(remaining < delay ? remaining : delay, lifetime);
                if (lifetime.IsCancellationRequested || stillConnected?.Invoke() == false) return;
                if (!await IsActiveAsync(lifetime)) break;
            }
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { return; }
        catch (Exception) { /* Unexpected validation failure also closes the lease. */ }
        if (!lifetime.IsCancellationRequested) invalidate();
    }

    public override string ToString() => nameof(AuthGateConnectionLease);
}

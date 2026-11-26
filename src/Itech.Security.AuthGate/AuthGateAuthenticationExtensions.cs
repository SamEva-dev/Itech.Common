using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace Itech.Security.AuthGate;

public static class AuthGateAuthenticationExtensions
{
    private sealed class OidcClientRegistrationMarker { }

    public static IServiceCollection AddAuthGateServiceClients(this IServiceCollection services) =>
        AuthGateServiceClient.AddClients(services);

    /// <summary>Registers a dedicated OIDC scheme, without changing application authorization policies or default schemes.</summary>
    public static AuthenticationBuilder AddAuthGateOidc(this AuthenticationBuilder builder,
        string scheme, AuthGateOidcSettings settings) =>
        AddOidcCore(builder, scheme, settings, platform: false);

    /// <summary>Requires a live platform authorization proof, never the ordinary session endpoint.</summary>
    public static AuthenticationBuilder AddAuthGatePlatformOidc(this AuthenticationBuilder builder,
        string scheme, AuthGateOidcSettings settings)
    {
        if (settings.AllowMissingClientBinding)
            throw new InvalidOperationException("Platform OIDC requires a dedicated client binding.");
        return AddOidcCore(builder, scheme, settings, platform: true);
    }

    private static AuthenticationBuilder AddOidcCore(AuthenticationBuilder builder,
        string scheme, AuthGateOidcSettings settings, bool platform)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scheme);
        settings.Validate();
        builder.Services.AddAuthGateServiceClients();
        builder.Services.TryAddSingleton<AuthGateSessionValidator>();
        builder.Services.TryAddSingleton<AuthGateAuthorizationClient>();
        if (!builder.Services.Any(x => x.ServiceType == typeof(OidcClientRegistrationMarker)))
        {
            builder.Services.AddSingleton(new OidcClientRegistrationMarker());
            builder.Services.AddHttpClient(AuthGateAuthorizationClient.HttpClientName, client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.MaxResponseContentBufferSize = 64 * 1024;
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false, UseCookies = false
            }).WithAuthGateTransport("authorization", retryReads: true);
            builder.Services.AddHttpClient(AuthGateSessionValidator.HttpClientName, client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.MaxResponseContentBufferSize = 16 * 1024;
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false, UseCookies = false
            }).WithAuthGateTransport("session", retryReads: true);
        }
        return builder.AddJwtBearer(scheme, options =>
        {
            options.Authority = settings.Issuer.TrimEnd('/');
            options.RequireHttpsMetadata = true;
            options.MapInboundClaims = false;
            options.SaveToken = true; // Only on the per-request authentication ticket; never persist to a cookie.
            options.IncludeErrorDetails = false;
            options.BackchannelTimeout = TimeSpan.FromSeconds(10);
            options.BackchannelHttpHandler = new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false };
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidIssuer = settings.Issuer,
                ValidateAudience = true, ValidAudience = settings.Audience,
                IgnoreTrailingSlashWhenValidatingAudience = false,
                ValidateIssuerSigningKey = true, RequireSignedTokens = true,
                ValidateLifetime = true, RequireExpirationTime = true,
                ValidTypes = ["at+jwt"], ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                ClockSkew = TimeSpan.Zero, NameClaimType = "sub", RoleClaimType = "role"
            };
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var tokenContext = context.Principal is null ? null : AuthGateTokenContext.Resolve(context.Principal, settings);
                    if (tokenContext is null)
                    {
                        context.Fail("Invalid AuthGate token context.");
                        return;
                    }
                    var validator = context.HttpContext.RequestServices.GetRequiredService<AuthGateSessionValidator>();
                    // Forward the exact token already validated by JwtBearer, including a token
                    // selected by a consumer's restricted SignalR OnMessageReceived handler.
                    // Never re-read untrusted query/header credentials at this stage.
                    var token = context.SecurityToken switch
                    {
                        JsonWebToken jwt => jwt.EncodedToken,
                        JwtSecurityToken jwt => jwt.RawData,
                        _ => null
                    };
                    if (string.IsNullOrWhiteSpace(token))
                    {
                        context.Fail("Missing validated access token.");
                        return;
                    }
                    var result = platform
                        ? await validator.ValidatePlatformAsync(settings, "Bearer " + token, tokenContext.UserId,
                            context.HttpContext.RequestAborted)
                        : await validator.ValidateAsync(settings, "Bearer " + token, context.HttpContext.RequestAborted);
                    if (result != AuthGateSessionState.Active) context.Fail("AuthGate session invalid or unavailable.");
                }
            };
        });
    }
}

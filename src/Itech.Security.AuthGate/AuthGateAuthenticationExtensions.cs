using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace Itech.Security.AuthGate;

public static class AuthGateAuthenticationExtensions
{
    /// <summary>Registers a dedicated OIDC scheme, without changing application authorization policies or default schemes.</summary>
    public static AuthenticationBuilder AddAuthGateOidc(this AuthenticationBuilder builder,
        string scheme, AuthGateOidcSettings settings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scheme);
        settings.Validate();
        builder.Services.TryAddSingleton<AuthGateSessionValidator>();
        builder.Services.AddHttpClient(AuthGateSessionValidator.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.MaxResponseContentBufferSize = 16 * 1024;
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = false, UseCookies = false
        });
        return builder.AddJwtBearer(scheme, options =>
        {
            options.Authority = settings.Issuer.TrimEnd('/');
            options.RequireHttpsMetadata = true;
            options.MapInboundClaims = false;
            options.SaveToken = false;
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
                    if (context.Principal is null || AuthGateTokenContext.Resolve(context.Principal, settings) is null)
                    {
                        context.Fail("Invalid AuthGate token context.");
                        return;
                    }
                    var validator = context.HttpContext.RequestServices.GetRequiredService<AuthGateSessionValidator>();
                    var result = await validator.ValidateAsync(settings, context.Request.Headers.Authorization.ToString(),
                        context.HttpContext.RequestAborted);
                    if (result != AuthGateSessionState.Active) context.Fail("AuthGate session invalid or unavailable.");
                }
            };
        });
    }
}

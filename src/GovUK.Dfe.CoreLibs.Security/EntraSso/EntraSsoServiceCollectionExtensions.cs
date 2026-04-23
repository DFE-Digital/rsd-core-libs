using GovUK.Dfe.CoreLibs.Security.Configurations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace GovUK.Dfe.CoreLibs.Security.EntraSso;

/// <summary>
/// Extension methods for registering Microsoft Entra ID (Azure AD) SSO authentication services
/// </summary>
public static class EntraSsoServiceCollectionExtensions
{
    /// <summary>
    /// Adds interactive Entra SSO (OpenID Connect) authentication for web applications.
    /// Registers an OIDC handler with a dedicated scheme name to coexist with DfE Sign-In.
    /// </summary>
    /// <param name="builder">The authentication builder to extend</param>
    /// <param name="configuration">Application configuration containing Entra SSO settings</param>
    /// <param name="sectionName">Configuration section name (defaults to "EntraSso")</param>
    /// <param name="customEvents">Optional custom OpenID Connect events</param>
    /// <returns>The authentication builder for chaining</returns>
    public static AuthenticationBuilder AddEntraSso(
        this AuthenticationBuilder builder,
        IConfiguration configuration,
        string sectionName = EntraSsoDefaults.ConfigurationSection,
        OpenIdConnectEvents? customEvents = null)
    {
        var section = configuration.GetSection(sectionName);
        builder.Services.Configure<EntraSsoOptions>(section);

        var opts = section.Get<EntraSsoOptions>()
            ?? throw new InvalidOperationException($"Missing '{sectionName}' configuration.");

        if (!opts.Enabled)
        {
            return builder;
        }

        return builder.AddOpenIdConnect(EntraSsoDefaults.AuthenticationScheme, "Microsoft Entra ID", oidc =>
        {
            oidc.Authority = opts.Authority;
            oidc.ClientId = opts.ClientId;
            oidc.ClientSecret = opts.ClientSecret;
            oidc.RequireHttpsMetadata = opts.RequireHttpsMetadata;
            oidc.ResponseType = opts.ResponseType;
            oidc.GetClaimsFromUserInfoEndpoint = opts.GetClaimsFromUserInfoEndpoint;
            oidc.SaveTokens = opts.SaveTokens;
            oidc.UseTokenLifetime = opts.UseTokenLifetime;
            oidc.CallbackPath = opts.CallbackPath;
            oidc.SignedOutCallbackPath = opts.SignedOutCallbackPath;

            oidc.TokenValidationParameters = new TokenValidationParameters
            {
                NameClaimType = opts.NameClaimType,
                ValidateIssuer = true
            };

            oidc.Scope.Clear();
            foreach (var scope in opts.Scopes)
                oidc.Scope.Add(scope);

            oidc.Events = customEvents ?? new OpenIdConnectEvents();

            var originalRedirectHandler = oidc.Events.OnRedirectToIdentityProvider;
            oidc.Events.OnRedirectToIdentityProvider = async ctx =>
            {
                if (originalRedirectHandler != null)
                    await originalRedirectHandler(ctx);
            };
        });
    }

    /// <summary>
    /// Adds Entra SSO JWT bearer token validation for API projects.
    /// Validates tokens issued by Microsoft Entra ID using standard OIDC discovery.
    /// </summary>
    /// <param name="builder">The authentication builder to extend</param>
    /// <param name="configuration">Application configuration containing Entra SSO settings</param>
    /// <param name="schemeName">The bearer authentication scheme name (defaults to EntraSsoDefaults.BearerScheme)</param>
    /// <param name="sectionName">Configuration section name (defaults to "EntraSso")</param>
    /// <param name="jwtBearerEvents">Optional JWT bearer events for custom validation logic</param>
    /// <returns>The authentication builder for chaining</returns>
    public static AuthenticationBuilder AddEntraSsoTokenValidation(
        this AuthenticationBuilder builder,
        IConfiguration configuration,
        string schemeName = EntraSsoDefaults.BearerScheme,
        string sectionName = EntraSsoDefaults.ConfigurationSection,
        JwtBearerEvents? jwtBearerEvents = null)
    {
        var section = configuration.GetSection(sectionName);
        builder.Services.Configure<EntraSsoOptions>(section);

        var opts = section.Get<EntraSsoOptions>()
            ?? throw new InvalidOperationException($"Missing '{sectionName}' configuration.");

        var audience = opts.Audience ?? $"api://{opts.ClientId}";
        var instance = opts.Instance.TrimEnd('/');
        var tenantId = opts.TenantId;

        return builder.AddJwtBearer(schemeName, jwt =>
        {
            jwt.Authority = $"{instance}/{tenantId}/v2.0";

            jwt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuers = new[]
                {
                    $"{instance}/{tenantId}/v2.0",
                    $"https://sts.windows.net/{tenantId}/",
                    $"https://login.microsoftonline.com/{tenantId}/v2.0"
                },
                ValidateAudience = true,
                ValidAudiences = new[] { audience, opts.ClientId, $"api://{opts.ClientId}" },
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true
            };

            if (jwtBearerEvents != null)
                jwt.Events = jwtBearerEvents;
        });
    }

    /// <summary>
    /// Registers Entra SSO options from configuration for dependency injection
    /// without adding any authentication handlers. Useful when handlers are configured
    /// separately but options need to be available via IOptions&lt;EntraSsoOptions&gt;.
    /// </summary>
    /// <param name="services">The service collection to extend</param>
    /// <param name="configuration">Application configuration containing Entra SSO settings</param>
    /// <param name="sectionName">Configuration section name (defaults to "EntraSso")</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection ConfigureEntraSso(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = EntraSsoDefaults.ConfigurationSection)
    {
        services.Configure<EntraSsoOptions>(configuration.GetSection(sectionName));
        return services;
    }
}

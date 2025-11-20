using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GovUK.Dfe.CoreLibs.Security.OpenIdConnect
{
    public static class OpenIdConnectServiceCollectionExtensions
    {
        /// <summary>
        /// Adds OIDC handler with optional custom event handlers.
        /// </summary>
        public static AuthenticationBuilder AddCustomOpenIdConnect(
            this AuthenticationBuilder builder,
            IConfiguration configuration,
            string sectionName = "DfESignIn",
            OpenIdConnectEvents? customEvents = null)
        {
            var section = configuration.GetSection(sectionName);
            builder.Services.Configure<Configurations.OpenIdConnectOptions>(section);

            var opts = section.Get<Configurations.OpenIdConnectOptions>()
                ?? throw new InvalidOperationException($"Missing '{sectionName}' configuration.");

            return builder.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, oidc =>
            {
                oidc.Authority = opts.Authority;
                oidc.ClientId = opts.ClientId;
                oidc.ClientSecret = opts.ClientSecret;
                oidc.RequireHttpsMetadata = opts.RequireHttpsMetadata;
                oidc.ResponseType = opts.ResponseType;
                oidc.GetClaimsFromUserInfoEndpoint = opts.GetClaimsFromUserInfoEndpoint;
                oidc.TokenValidationParameters.NameClaimType = opts.NameClaimType;
                oidc.SaveTokens = opts.SaveTokens;
                oidc.UseTokenLifetime = opts.UseTokenLifetime;

                oidc.Scope.Clear();
                foreach (var scope in opts.Scopes)
                    oidc.Scope.Add(scope);

                // Use provided events if supplied, otherwise default
                oidc.Events = customEvents ?? new OpenIdConnectEvents();

                // Always ensure redirect logic runs, even if custom events provided
                var originalRedirectHandler = oidc.Events.OnRedirectToIdentityProvider;
                oidc.Events.OnRedirectToIdentityProvider = async ctx =>
                {
                    if (!string.IsNullOrEmpty(opts.RedirectUri))
                        ctx.ProtocolMessage.RedirectUri = opts.RedirectUri;

                    ctx.ProtocolMessage.Prompt = opts.Prompt;

                    if (originalRedirectHandler != null)
                        await originalRedirectHandler(ctx);
                };
            });
        }
    }
}

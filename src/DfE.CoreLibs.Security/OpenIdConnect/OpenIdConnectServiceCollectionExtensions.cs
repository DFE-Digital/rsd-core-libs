using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DfE.CoreLibs.Security.OpenIdConnect
{
    public static class OpenIdConnectServiceCollectionExtensions
    {
        /// <summary>
        /// Adds OIDC handler.
        /// </summary>
        public static AuthenticationBuilder AddCustomOpenIdConnect(
            this AuthenticationBuilder builder,
            IConfiguration configuration,
            string sectionName = "DfESignIn")
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

                oidc.Events = new OpenIdConnectEvents
                {
                    OnRedirectToIdentityProvider = ctx =>
                    {
                        if (!string.IsNullOrEmpty(opts.RedirectUri))
                            ctx.ProtocolMessage.RedirectUri = opts.RedirectUri;

                        ctx.ProtocolMessage.Prompt = opts.Prompt;
                        return Task.CompletedTask;
                    }
                };
            });
        }
    }
}

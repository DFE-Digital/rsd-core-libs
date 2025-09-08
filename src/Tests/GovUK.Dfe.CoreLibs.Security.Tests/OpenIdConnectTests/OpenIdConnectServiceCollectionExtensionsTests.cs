using GovUK.Dfe.CoreLibs.Security.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace GovUK.Dfe.CoreLibs.Security.Tests.OpenIdConnectTests
{
    public class OpenIdConnectServiceCollectionExtensionsTests
    {
        private const string SectionName = "SignIn";

        private IServiceProvider BuildServiceProvider(Dictionary<string, string> inMemorySettings)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var services = new ServiceCollection();
            var authBuilder = services.AddAuthentication();
            authBuilder.AddCustomOpenIdConnect(configuration, SectionName);

            return services.BuildServiceProvider();
        }

        [Fact]
        public void AddDfESignInIdOnly_BindsOptionsFromConfiguration()
        {
            var settings = new Dictionary<string, string>
            {
                [$"{SectionName}:Authority"] = "https://example.com/oidc",
                [$"{SectionName}:ClientId"] = "my-client",
                [$"{SectionName}:ClientSecret"] = "topsecret",
                [$"{SectionName}:RedirectUri"] = "https://app/callback",
                [$"{SectionName}:Prompt"] = "login",
                [$"{SectionName}:ResponseType"] = "code",
                [$"{SectionName}:RequireHttpsMetadata"] = "false",
                [$"{SectionName}:GetClaimsFromUserInfoEndpoint"] = "false",
                [$"{SectionName}:SaveTokens"] = "false",
                [$"{SectionName}:UseTokenLifetime"] = "false",
                [$"{SectionName}:NameClaimType"] = "email-claim",
                [$"{SectionName}:Scopes:0"] = "openid",
                [$"{SectionName}:Scopes:1"] = "profile",
            };

            var provider = BuildServiceProvider(settings);
            var opts = provider.GetRequiredService<IOptions<Configurations.OpenIdConnectOptions>>().Value;

            Assert.Equal("https://example.com/oidc", opts.Authority);
            Assert.Equal("my-client", opts.ClientId);
            Assert.Equal("topsecret", opts.ClientSecret);
            Assert.Equal("https://app/callback", opts.RedirectUri);
            Assert.Equal("login", opts.Prompt);
            Assert.Equal("code", opts.ResponseType);
            Assert.False(opts.RequireHttpsMetadata);
            Assert.False(opts.GetClaimsFromUserInfoEndpoint);
            Assert.False(opts.SaveTokens);
            Assert.False(opts.UseTokenLifetime);
            Assert.Equal("email-claim", opts.NameClaimType);
            Assert.Equal(new List<string> { "openid", "profile" }, opts.Scopes);
        }

        [Fact]
        public void AddDfESignInIdOnly_RegistersOpenIdConnectOptionsCorrectly()
        {
            var settings = new Dictionary<string, string>
            {
                [$"{SectionName}:Authority"] = "https://example.com/oidc2",
                [$"{SectionName}:ClientId"] = "client2",
                [$"{SectionName}:ClientSecret"] = "secret2",
                [$"{SectionName}:Scopes:0"] = "openid",
                [$"{SectionName}:Scopes:1"] = "email",
            };

            var provider = BuildServiceProvider(settings);
            var monitor = provider.GetRequiredService<IOptionsMonitor<Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions>>();
            var oidcOpts = monitor.Get(OpenIdConnectDefaults.AuthenticationScheme);

            Assert.Equal("https://example.com/oidc2", oidcOpts.Authority);
            Assert.Equal("client2", oidcOpts.ClientId);
            Assert.Equal("secret2", oidcOpts.ClientSecret);
            Assert.True(oidcOpts.RequireHttpsMetadata);
            Assert.Equal("code", oidcOpts.ResponseType);
            Assert.True(oidcOpts.GetClaimsFromUserInfoEndpoint);
            Assert.True(oidcOpts.SaveTokens);
            Assert.True(oidcOpts.UseTokenLifetime);
            Assert.Equal("email", oidcOpts.TokenValidationParameters.NameClaimType);
            Assert.Equal(new List<string> { "openid", "email" }, oidcOpts.Scope);
            Assert.NotNull(oidcOpts.Events);
            Assert.NotNull(oidcOpts.Events.OnRedirectToIdentityProvider);
        }

        [Fact]
        public async Task AddDfESignInIdOnly_OnRedirectToIdentityProvider_EventAppliesOverrides()
        {
            // Arrange
            var settings = new Dictionary<string, string>
            {
                [$"{SectionName}:Authority"] = "https://auth",
                [$"{SectionName}:ClientId"] = "cid",
                [$"{SectionName}:ClientSecret"] = "csec",
                [$"{SectionName}:RedirectUri"] = "https://override/cb",
                [$"{SectionName}:Prompt"] = "custom-prompt",
                [$"{SectionName}:Scopes:0"] = "openid",
            };
            var provider = BuildServiceProvider(settings);

            var oidcOpts = provider
                .GetRequiredService<IOptionsMonitor<Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions>>()
                .Get(OpenIdConnectDefaults.AuthenticationScheme);

            var httpContext = new DefaultHttpContext();
            var scheme = new AuthenticationScheme(
                OpenIdConnectDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme,
                typeof(OpenIdConnectHandler));
            var props = new AuthenticationProperties();

            var redirectCtx = new RedirectContext(
                httpContext,
                scheme,
                oidcOpts,
                props)
            {
                ProtocolMessage = new OpenIdConnectMessage
                {
                    RedirectUri = "https://example",
                    Prompt = null
                }
            };

            await oidcOpts.Events.RedirectToIdentityProvider(redirectCtx);

            Assert.Equal("https://override/cb", redirectCtx.ProtocolMessage.RedirectUri);
            Assert.Equal("custom-prompt", redirectCtx.ProtocolMessage.Prompt);
        }

    }
}

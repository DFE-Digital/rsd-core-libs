using GovUK.Dfe.CoreLibs.Security.Configurations;
using GovUK.Dfe.CoreLibs.Security.Interfaces;
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

        private IServiceProvider BuildServiceProvider(Dictionary<string, string?> inMemorySettings)
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
            var settings = new Dictionary<string, string?>
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
            var settings = new Dictionary<string, string?>
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
            var settings = new Dictionary<string, string?>
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

        #region AddExternalIdentityValidation Tests

        [Fact]
        public void AddExternalIdentityValidation_SingleProvider_RegistersValidator()
        {
            var settings = new Dictionary<string, string?>
            {
                ["DfESignIn:Issuer"] = "https://idp.example.com/",
                ["DfESignIn:DiscoveryEndpoint"] = "https://idp.example.com/.well-known/openid-configuration",
                ["DfESignIn:ClientId"] = "test-client",
                ["DfESignIn:ValidateIssuer"] = "true",
                ["DfESignIn:ValidateAudience"] = "false",
                ["DfESignIn:ValidateLifetime"] = "true",
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();

            var services = new ServiceCollection();
            services.AddExternalIdentityValidation(configuration);

            var provider = services.BuildServiceProvider();

            // Verify validator is registered
            var validator = provider.GetService<IExternalIdentityValidator>();
            Assert.NotNull(validator);
            Assert.IsType<ExternalIdentityValidator>(validator);

            // Verify options are bound
            var opts = provider.GetRequiredService<IOptions<Configurations.OpenIdConnectOptions>>().Value;
            Assert.Equal("https://idp.example.com/", opts.Issuer);
            Assert.Equal("https://idp.example.com/.well-known/openid-configuration", opts.DiscoveryEndpoint);
            Assert.Equal("test-client", opts.ClientId);
        }

        [Fact]
        public void AddExternalIdentityValidation_SingleProvider_CustomSectionName_RegistersValidator()
        {
            var settings = new Dictionary<string, string?>
            {
                ["CustomOidc:Issuer"] = "https://custom.example.com/",
                ["CustomOidc:DiscoveryEndpoint"] = "https://custom.example.com/.well-known/openid-configuration",
                ["CustomOidc:ClientId"] = "custom-client",
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();

            var services = new ServiceCollection();
            services.AddExternalIdentityValidation(configuration, sectionName: "CustomOidc");

            var provider = services.BuildServiceProvider();

            var opts = provider.GetRequiredService<IOptions<Configurations.OpenIdConnectOptions>>().Value;
            Assert.Equal("https://custom.example.com/", opts.Issuer);
            Assert.Equal("custom-client", opts.ClientId);
        }

        [Fact]
        public void AddExternalIdentityValidation_MultiProvider_RegistersValidator()
        {
            var settings = new Dictionary<string, string?>
            {
                // Empty - multi-provider doesn't need config section for providers
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();

            var services = new ServiceCollection();
            services.AddExternalIdentityValidation(configuration, multiOpts =>
            {
                multiOpts.Providers.Add(new Configurations.OpenIdConnectOptions
                {
                    Issuer = "https://tenant1.example.com/",
                    ClientId = "client-tenant1",
                    DiscoveryEndpoint = "https://tenant1.example.com/.well-known/openid-configuration",
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true
                });
                multiOpts.Providers.Add(new Configurations.OpenIdConnectOptions
                {
                    Issuer = "https://tenant2.example.com/",
                    ClientId = "client-tenant2",
                    DiscoveryEndpoint = "https://tenant2.example.com/.well-known/openid-configuration",
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true
                });
            });

            var provider = services.BuildServiceProvider();

            // Verify validator is registered
            var validator = provider.GetService<IExternalIdentityValidator>();
            Assert.NotNull(validator);
            Assert.IsType<ExternalIdentityValidator>(validator);

            // Verify multi-provider options are configured
            var multiProviderOpts = provider.GetRequiredService<IOptions<MultiProviderOpenIdConnectOptions>>().Value;
            Assert.Equal(2, multiProviderOpts.Providers.Count);
            Assert.Equal("https://tenant1.example.com/", multiProviderOpts.Providers[0].Issuer);
            Assert.Equal("client-tenant1", multiProviderOpts.Providers[0].ClientId);
            Assert.Equal("https://tenant2.example.com/", multiProviderOpts.Providers[1].Issuer);
            Assert.Equal("client-tenant2", multiProviderOpts.Providers[1].ClientId);
        }

        [Fact]
        public void AddExternalIdentityValidation_MultiProvider_IsInMultiProviderMode()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>())
                .Build();

            var services = new ServiceCollection();
            services.AddExternalIdentityValidation(configuration, multiOpts =>
            {
                multiOpts.Providers.Add(new Configurations.OpenIdConnectOptions
                {
                    Issuer = "https://tenant1.example.com/",
                    ClientId = "client-tenant1",
                    DiscoveryEndpoint = "https://tenant1.example.com/.well-known/openid-configuration"
                });
            });

            var provider = services.BuildServiceProvider();

            var validator = provider.GetRequiredService<IExternalIdentityValidator>() as ExternalIdentityValidator;
            Assert.NotNull(validator);
            Assert.True(validator.IsMultiProviderMode);
            Assert.Equal(1, validator.ProviderCount);
        }

        [Fact]
        public void AddExternalIdentityValidation_MultiProvider_WithTestAuth_ConfiguresTestOptions()
        {
            var settings = new Dictionary<string, string?>
            {
                ["TestAuthentication:Enabled"] = "true",
                ["TestAuthentication:JwtSigningKey"] = "test-key-that-is-long-enough-32ch",
                ["TestAuthentication:JwtIssuer"] = "test-issuer",
                ["TestAuthentication:JwtAudience"] = "test-audience",
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();

            var services = new ServiceCollection();
            services.AddExternalIdentityValidation(configuration, multiOpts =>
            {
                multiOpts.Providers.Add(new Configurations.OpenIdConnectOptions
                {
                    Issuer = "https://tenant1.example.com/",
                    DiscoveryEndpoint = "https://tenant1.example.com/.well-known/openid-configuration"
                });
            });

            var provider = services.BuildServiceProvider();

            var testOpts = provider.GetRequiredService<IOptions<TestAuthenticationOptions>>().Value;
            Assert.True(testOpts.Enabled);
            Assert.Equal("test-key-that-is-long-enough-32ch", testOpts.JwtSigningKey);
            Assert.Equal("test-issuer", testOpts.JwtIssuer);
            Assert.Equal("test-audience", testOpts.JwtAudience);
        }

        [Fact]
        public void AddExternalIdentityValidation_MultiProvider_WithInternalAuth_ConfiguresInternalOptions()
        {
            var settings = new Dictionary<string, string?>
            {
                ["InternalServiceAuth:SecretKey"] = "internal-secret-key-long-enough-32",
                ["InternalServiceAuth:Issuer"] = "internal-issuer",
                ["InternalServiceAuth:Audience"] = "internal-audience",
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();

            var services = new ServiceCollection();
            services.AddExternalIdentityValidation(configuration, multiOpts =>
            {
                multiOpts.Providers.Add(new Configurations.OpenIdConnectOptions
                {
                    Issuer = "https://tenant1.example.com/",
                    DiscoveryEndpoint = "https://tenant1.example.com/.well-known/openid-configuration"
                });
            });

            var provider = services.BuildServiceProvider();

            var internalOpts = provider.GetRequiredService<IOptions<InternalServiceAuthOptions>>().Value;
            Assert.Equal("internal-secret-key-long-enough-32", internalOpts.SecretKey);
            Assert.Equal("internal-issuer", internalOpts.Issuer);
            Assert.Equal("internal-audience", internalOpts.Audience);
        }

        [Fact]
        public void AddExternalIdentityValidation_MultiProvider_EmptyProviders_FallsBackToSingleMode()
        {
            var settings = new Dictionary<string, string?>
            {
                ["DfESignIn:Issuer"] = "https://fallback.example.com/",
                ["DfESignIn:DiscoveryEndpoint"] = "https://fallback.example.com/.well-known/openid-configuration",
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();

            var services = new ServiceCollection();
            // Configure with empty providers list
            services.AddExternalIdentityValidation(configuration, multiOpts =>
            {
                // No providers added - should fall back to placeholder/single mode
            });

            var provider = services.BuildServiceProvider();

            var multiProviderOpts = provider.GetRequiredService<IOptions<MultiProviderOpenIdConnectOptions>>().Value;
            Assert.Empty(multiProviderOpts.Providers);

            // Validator should still be registered (will use single-provider fallback)
            var validator = provider.GetService<IExternalIdentityValidator>();
            Assert.NotNull(validator);
        }

        #endregion
    }
}
using System.Security.Claims;
using GovUK.Dfe.CoreLibs.Security.Configurations;
using GovUK.Dfe.CoreLibs.Security.EntraSso;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using MsOidcOptions = Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace GovUK.Dfe.CoreLibs.Security.Tests.EntraSsoTests
{
    public class EntraSsoServiceCollectionExtensionsTests
    {
        private const string TenantId = "9c7d9dd3-840c-4b3f-818e-552865082e16";
        private const string ClientId = "test-client-id";
        private const string ClientSecret = "test-client-secret";

        private static Dictionary<string, string?> CreateEnabledSettings(string section = "EntraSso")
        {
            return new Dictionary<string, string?>
            {
                [$"{section}:Enabled"] = "true",
                [$"{section}:Instance"] = "https://login.microsoftonline.com/",
                [$"{section}:TenantId"] = TenantId,
                [$"{section}:ClientId"] = ClientId,
                [$"{section}:ClientSecret"] = ClientSecret,
                [$"{section}:CallbackPath"] = "/signin-entra",
                [$"{section}:SignedOutCallbackPath"] = "/signout-callback-entra",
                [$"{section}:ResponseType"] = "code",
                [$"{section}:SaveTokens"] = "true",
                [$"{section}:GetClaimsFromUserInfoEndpoint"] = "true",
                [$"{section}:RequireHttpsMetadata"] = "true",
                [$"{section}:UseTokenLifetime"] = "true",
                [$"{section}:NameClaimType"] = "preferred_username",
                [$"{section}:Scopes:0"] = "openid",
                [$"{section}:Scopes:1"] = "profile",
                [$"{section}:Scopes:2"] = "email",
                [$"{section}:Audience"] = $"api://{ClientId}"
            };
        }

        private static Dictionary<string, string?> CreateDisabledSettings(string section = "EntraSso")
        {
            return new Dictionary<string, string?>
            {
                [$"{section}:Enabled"] = "false",
                [$"{section}:Instance"] = "https://login.microsoftonline.com/",
                [$"{section}:TenantId"] = TenantId,
                [$"{section}:ClientId"] = ClientId,
                [$"{section}:ClientSecret"] = ClientSecret,
            };
        }

        #region AddEntraSso Tests

        [Fact]
        public async Task AddEntraSso_WhenEnabled_RegistersOidcScheme()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(CreateEnabledSettings())
                .Build();

            var services = new ServiceCollection();
            var authBuilder = services.AddAuthentication();
            authBuilder.AddEntraSso(configuration);

            var provider = services.BuildServiceProvider();

            var schemeProvider = provider.GetRequiredService<IAuthenticationSchemeProvider>();
            var scheme = await schemeProvider.GetSchemeAsync(EntraSsoDefaults.AuthenticationScheme);

            Assert.NotNull(scheme);
            Assert.Equal(EntraSsoDefaults.AuthenticationScheme, scheme.Name);
            Assert.Equal("Microsoft Entra ID", scheme.DisplayName);
        }

        [Fact]
        public async Task AddEntraSso_WhenDisabled_DoesNotRegisterOidcScheme()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(CreateDisabledSettings())
                .Build();

            var services = new ServiceCollection();
            var authBuilder = services.AddAuthentication();
            authBuilder.AddEntraSso(configuration);

            var provider = services.BuildServiceProvider();

            var schemeProvider = provider.GetRequiredService<IAuthenticationSchemeProvider>();
            var scheme = await schemeProvider.GetSchemeAsync(EntraSsoDefaults.AuthenticationScheme);

            Assert.Null(scheme);
        }

        [Fact]
        public void AddEntraSso_BindsEntraSsoOptionsFromConfiguration()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(CreateEnabledSettings())
                .Build();

            var services = new ServiceCollection();
            var authBuilder = services.AddAuthentication();
            authBuilder.AddEntraSso(configuration);

            var provider = services.BuildServiceProvider();

            var opts = provider.GetRequiredService<IOptions<EntraSsoOptions>>().Value;

            Assert.True(opts.Enabled);
            Assert.Equal("https://login.microsoftonline.com/", opts.Instance);
            Assert.Equal(TenantId, opts.TenantId);
            Assert.Equal(ClientId, opts.ClientId);
            Assert.Equal(ClientSecret, opts.ClientSecret);
            Assert.Equal("/signin-entra", opts.CallbackPath);
            Assert.Equal("preferred_username", opts.NameClaimType);
            Assert.Contains("openid", opts.Scopes);
            Assert.Contains("profile", opts.Scopes);
            Assert.Contains("email", opts.Scopes);
        }

        [Fact]
        public void AddEntraSso_WhenEnabled_ConfiguresOidcHandlerOptionsCorrectly()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(CreateEnabledSettings())
                .Build();

            var services = new ServiceCollection();
            var authBuilder = services.AddAuthentication();
            authBuilder.AddEntraSso(configuration);

            var provider = services.BuildServiceProvider();

            var monitor = provider.GetRequiredService<IOptionsMonitor<MsOidcOptions>>();
            var oidcOpts = monitor.Get(EntraSsoDefaults.AuthenticationScheme);

            Assert.Equal($"https://login.microsoftonline.com/{TenantId}/v2.0", oidcOpts.Authority);
            Assert.Equal(ClientId, oidcOpts.ClientId);
            Assert.Equal(ClientSecret, oidcOpts.ClientSecret);
            Assert.True(oidcOpts.RequireHttpsMetadata);
            Assert.Equal("code", oidcOpts.ResponseType);
            Assert.True(oidcOpts.GetClaimsFromUserInfoEndpoint);
            Assert.True(oidcOpts.SaveTokens);
            Assert.True(oidcOpts.UseTokenLifetime);
            Assert.Equal("preferred_username", oidcOpts.TokenValidationParameters.NameClaimType);
            Assert.Equal(new List<string> { "openid", "profile", "email" }, oidcOpts.Scope);
            Assert.Equal("/signin-entra", oidcOpts.CallbackPath);
            Assert.Equal("/signout-callback-entra", oidcOpts.SignedOutCallbackPath);
            Assert.NotNull(oidcOpts.Events);
        }

        [Fact]
        public void AddEntraSso_WithCustomSectionName_BindsFromCorrectSection()
        {
            var settings = CreateEnabledSettings("CustomEntra");
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();

            var services = new ServiceCollection();
            var authBuilder = services.AddAuthentication();
            authBuilder.AddEntraSso(configuration, sectionName: "CustomEntra");

            var provider = services.BuildServiceProvider();

            var opts = provider.GetRequiredService<IOptions<EntraSsoOptions>>().Value;
            Assert.True(opts.Enabled);
            Assert.Equal(ClientId, opts.ClientId);
        }

        [Fact]
        public void AddEntraSso_WhenMissingConfiguration_ThrowsInvalidOperationException()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>())
                .Build();

            var services = new ServiceCollection();
            var authBuilder = services.AddAuthentication();

            Assert.Throws<InvalidOperationException>(() =>
                authBuilder.AddEntraSso(configuration, sectionName: "NonExistent"));
        }

        [Fact]
        public void AddEntraSso_WithCustomEvents_AppliesEvents()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(CreateEnabledSettings())
                .Build();

            var customEvents = new OpenIdConnectEvents
            {
                OnAuthenticationFailed = _ => Task.CompletedTask
            };

            var services = new ServiceCollection();
            var authBuilder = services.AddAuthentication();
            authBuilder.AddEntraSso(configuration, customEvents: customEvents);

            var provider = services.BuildServiceProvider();

            var monitor = provider.GetRequiredService<IOptionsMonitor<MsOidcOptions>>();
            var oidcOpts = monitor.Get(EntraSsoDefaults.AuthenticationScheme);

            Assert.NotNull(oidcOpts.Events);
            Assert.NotNull(oidcOpts.Events.OnAuthenticationFailed);
        }

        [Fact]
        public async Task AddEntraSso_DoesNotConflictWithStandardOidc()
        {
            var settings = new Dictionary<string, string?>(CreateEnabledSettings())
            {
                ["DfESignIn:Authority"] = "https://test-oidc.signin.education.gov.uk",
                ["DfESignIn:ClientId"] = "DfEClient",
                ["DfESignIn:ClientSecret"] = "secret",
                ["DfESignIn:Scopes:0"] = "openid"
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();

            var services = new ServiceCollection();
            var authBuilder = services.AddAuthentication();

            authBuilder.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, oidc =>
            {
                oidc.Authority = "https://test-oidc.signin.education.gov.uk";
                oidc.ClientId = "DfEClient";
            });
            authBuilder.AddEntraSso(configuration);

            var provider = services.BuildServiceProvider();
            var schemeProvider = provider.GetRequiredService<IAuthenticationSchemeProvider>();

            var dfeSiScheme = await schemeProvider.GetSchemeAsync(OpenIdConnectDefaults.AuthenticationScheme);
            var entraSsoScheme = await schemeProvider.GetSchemeAsync(EntraSsoDefaults.AuthenticationScheme);

            Assert.NotNull(dfeSiScheme);
            Assert.NotNull(entraSsoScheme);
            Assert.NotEqual(dfeSiScheme.Name, entraSsoScheme.Name);
        }

        #endregion

        #region AddEntraSsoTokenValidation Tests

        [Fact]
        public async Task AddEntraSsoTokenValidation_RegistersJwtBearerScheme()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(CreateEnabledSettings())
                .Build();

            var services = new ServiceCollection();
            var authBuilder = services.AddAuthentication();
            authBuilder.AddEntraSsoTokenValidation(configuration);

            var provider = services.BuildServiceProvider();

            var schemeProvider = provider.GetRequiredService<IAuthenticationSchemeProvider>();
            var scheme = await schemeProvider.GetSchemeAsync(EntraSsoDefaults.BearerScheme);

            Assert.NotNull(scheme);
            Assert.Equal(EntraSsoDefaults.BearerScheme, scheme.Name);
        }

        [Fact]
        public void AddEntraSsoTokenValidation_ConfiguresJwtBearerOptionsCorrectly()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(CreateEnabledSettings())
                .Build();

            var services = new ServiceCollection();
            var authBuilder = services.AddAuthentication();
            authBuilder.AddEntraSsoTokenValidation(configuration);

            var provider = services.BuildServiceProvider();

            var monitor = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
            var jwtOpts = monitor.Get(EntraSsoDefaults.BearerScheme);

            Assert.Equal($"https://login.microsoftonline.com/{TenantId}/v2.0", jwtOpts.Authority);
            Assert.True(jwtOpts.TokenValidationParameters.ValidateIssuer);
            Assert.True(jwtOpts.TokenValidationParameters.ValidateAudience);
            Assert.True(jwtOpts.TokenValidationParameters.ValidateLifetime);
            Assert.True(jwtOpts.TokenValidationParameters.ValidateIssuerSigningKey);
        }

        [Fact]
        public void AddEntraSsoTokenValidation_ConfiguresMultiFormatIssuers()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(CreateEnabledSettings())
                .Build();

            var services = new ServiceCollection();
            var authBuilder = services.AddAuthentication();
            authBuilder.AddEntraSsoTokenValidation(configuration);

            var provider = services.BuildServiceProvider();

            var monitor = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
            var jwtOpts = monitor.Get(EntraSsoDefaults.BearerScheme);

            var validIssuers = jwtOpts.TokenValidationParameters.ValidIssuers.ToList();
            Assert.Contains($"https://login.microsoftonline.com/{TenantId}/v2.0", validIssuers);
            Assert.Contains($"https://sts.windows.net/{TenantId}/", validIssuers);
            Assert.Equal(3, validIssuers.Count);
        }

        [Fact]
        public void AddEntraSsoTokenValidation_ConfiguresMultiFormatAudiences()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(CreateEnabledSettings())
                .Build();

            var services = new ServiceCollection();
            var authBuilder = services.AddAuthentication();
            authBuilder.AddEntraSsoTokenValidation(configuration);

            var provider = services.BuildServiceProvider();

            var monitor = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
            var jwtOpts = monitor.Get(EntraSsoDefaults.BearerScheme);

            var validAudiences = jwtOpts.TokenValidationParameters.ValidAudiences.ToList();
            Assert.Contains($"api://{ClientId}", validAudiences);
            Assert.Contains(ClientId, validAudiences);
            Assert.Equal(3, validAudiences.Count);
        }

        [Fact]
        public async Task AddEntraSsoTokenValidation_WithCustomSchemeName_UsesCustomName()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(CreateEnabledSettings())
                .Build();

            var services = new ServiceCollection();
            var authBuilder = services.AddAuthentication();
            authBuilder.AddEntraSsoTokenValidation(configuration, schemeName: "MyCustomEntra");

            var provider = services.BuildServiceProvider();

            var schemeProvider = provider.GetRequiredService<IAuthenticationSchemeProvider>();
            var scheme = await schemeProvider.GetSchemeAsync("MyCustomEntra");

            Assert.NotNull(scheme);
            Assert.Equal("MyCustomEntra", scheme.Name);
        }

        [Fact]
        public void AddEntraSsoTokenValidation_WithCustomEvents_AppliesEvents()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(CreateEnabledSettings())
                .Build();

            var customEvents = new JwtBearerEvents
            {
                OnTokenValidated = _ => Task.CompletedTask
            };

            var services = new ServiceCollection();
            var authBuilder = services.AddAuthentication();
            authBuilder.AddEntraSsoTokenValidation(configuration, jwtBearerEvents: customEvents);

            var provider = services.BuildServiceProvider();

            var monitor = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
            var jwtOpts = monitor.Get(EntraSsoDefaults.BearerScheme);

            Assert.NotNull(jwtOpts.Events);
            Assert.Same(customEvents, jwtOpts.Events);
        }

        [Fact]
        public void AddEntraSsoTokenValidation_WithNoAudience_FallsBackToApiPrefix()
        {
            var settings = new Dictionary<string, string?>
            {
                ["EntraSso:Enabled"] = "true",
                ["EntraSso:Instance"] = "https://login.microsoftonline.com/",
                ["EntraSso:TenantId"] = TenantId,
                ["EntraSso:ClientId"] = "my-client",
                ["EntraSso:ClientSecret"] = "secret",
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();

            var services = new ServiceCollection();
            var authBuilder = services.AddAuthentication();
            authBuilder.AddEntraSsoTokenValidation(configuration);

            var provider = services.BuildServiceProvider();

            var monitor = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
            var jwtOpts = monitor.Get(EntraSsoDefaults.BearerScheme);

            var validAudiences = jwtOpts.TokenValidationParameters.ValidAudiences.ToList();
            Assert.Contains("api://my-client", validAudiences);
        }

        [Fact]
        public void AddEntraSsoTokenValidation_WhenMissingConfiguration_ThrowsInvalidOperationException()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>())
                .Build();

            var services = new ServiceCollection();
            var authBuilder = services.AddAuthentication();

            Assert.Throws<InvalidOperationException>(() =>
                authBuilder.AddEntraSsoTokenValidation(configuration, sectionName: "NonExistent"));
        }

        #endregion

        #region ConfigureEntraSso Tests

        [Fact]
        public void ConfigureEntraSso_RegistersOptionsFromConfiguration()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(CreateEnabledSettings())
                .Build();

            var services = new ServiceCollection();
            services.ConfigureEntraSso(configuration);

            var provider = services.BuildServiceProvider();

            var opts = provider.GetRequiredService<IOptions<EntraSsoOptions>>().Value;

            Assert.True(opts.Enabled);
            Assert.Equal(TenantId, opts.TenantId);
            Assert.Equal(ClientId, opts.ClientId);
        }

        [Fact]
        public void ConfigureEntraSso_WithCustomSectionName_BindsCorrectly()
        {
            var settings = CreateEnabledSettings("MyEntra");

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();

            var services = new ServiceCollection();
            services.ConfigureEntraSso(configuration, sectionName: "MyEntra");

            var provider = services.BuildServiceProvider();

            var opts = provider.GetRequiredService<IOptions<EntraSsoOptions>>().Value;
            Assert.True(opts.Enabled);
            Assert.Equal(ClientId, opts.ClientId);
        }

        [Fact]
        public void ConfigureEntraSso_DoesNotRegisterAuthenticationHandlers()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(CreateEnabledSettings())
                .Build();

            var services = new ServiceCollection();
            services.ConfigureEntraSso(configuration);

            var provider = services.BuildServiceProvider();

            var schemeProvider = provider.GetService<IAuthenticationSchemeProvider>();
            Assert.Null(schemeProvider);
        }

        [Fact]
        public void ConfigureEntraSso_ReturnsSameServiceCollection()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(CreateEnabledSettings())
                .Build();

            var services = new ServiceCollection();
            var result = services.ConfigureEntraSso(configuration);

            Assert.Same(services, result);
        }

        #endregion

        #region ValidateGroupMembership Tests

        [Fact]
        public void ValidateGroupMembership_WithMatchingGroup_DoesNotThrow()
        {
            var groupId = "a1b2c3d4-e5f6-7890-abcd-ef1234567890";
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("groups", groupId),
                new Claim("groups", "00000000-0000-0000-0000-000000000001")
            }, "TestAuth"));

            var exception = Record.Exception(() =>
                EntraSsoServiceCollectionExtensions.ValidateGroupMembership(principal, groupId));

            Assert.Null(exception);
        }

        [Fact]
        public void ValidateGroupMembership_WithNoMatchingGroup_ThrowsUnauthorizedAccessException()
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("groups", "00000000-0000-0000-0000-000000000001"),
                new Claim("groups", "00000000-0000-0000-0000-000000000002")
            }, "TestAuth"));

            var ex = Assert.Throws<UnauthorizedAccessException>(() =>
                EntraSsoServiceCollectionExtensions.ValidateGroupMembership(
                    principal, "a1b2c3d4-e5f6-7890-abcd-ef1234567890"));

            Assert.Contains("a1b2c3d4-e5f6-7890-abcd-ef1234567890", ex.Message);
        }

        [Fact]
        public void ValidateGroupMembership_WithNoGroupClaims_ThrowsUnauthorizedAccessException()
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "testuser")
            }, "TestAuth"));

            Assert.Throws<UnauthorizedAccessException>(() =>
                EntraSsoServiceCollectionExtensions.ValidateGroupMembership(
                    principal, "a1b2c3d4-e5f6-7890-abcd-ef1234567890"));
        }

        [Fact]
        public void ValidateGroupMembership_WithNullPrincipal_ThrowsUnauthorizedAccessException()
        {
            Assert.Throws<UnauthorizedAccessException>(() =>
                EntraSsoServiceCollectionExtensions.ValidateGroupMembership(
                    null, "a1b2c3d4-e5f6-7890-abcd-ef1234567890"));
        }

        [Fact]
        public void ValidateGroupMembership_IsCaseInsensitive()
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("groups", "A1B2C3D4-E5F6-7890-ABCD-EF1234567890")
            }, "TestAuth"));

            var exception = Record.Exception(() =>
                EntraSsoServiceCollectionExtensions.ValidateGroupMembership(
                    principal, "a1b2c3d4-e5f6-7890-abcd-ef1234567890"));

            Assert.Null(exception);
        }

        [Fact]
        public void AddEntraSso_WithAllowedGroupId_ConfiguresGroupValidationEvent()
        {
            var settings = new Dictionary<string, string?>(CreateEnabledSettings())
            {
                ["EntraSso:AllowedGroupId"] = "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();

            var services = new ServiceCollection();
            var authBuilder = services.AddAuthentication();
            authBuilder.AddEntraSso(configuration);

            var provider = services.BuildServiceProvider();

            var monitor = provider.GetRequiredService<IOptionsMonitor<MsOidcOptions>>();
            var oidcOpts = monitor.Get(EntraSsoDefaults.AuthenticationScheme);

            Assert.NotNull(oidcOpts.Events);
            Assert.NotNull(oidcOpts.Events.OnTokenValidated);
        }

        [Fact]
        public void AddEntraSso_WithoutAllowedGroupId_DoesNotAddGroupValidation()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(CreateEnabledSettings())
                .Build();

            var services = new ServiceCollection();
            var authBuilder = services.AddAuthentication();
            authBuilder.AddEntraSso(configuration);

            var provider = services.BuildServiceProvider();

            var opts = provider.GetRequiredService<IOptions<EntraSsoOptions>>().Value;
            Assert.Null(opts.AllowedGroupId);
        }

        [Fact]
        public void AddEntraSsoTokenValidation_WithAllowedGroupId_ConfiguresGroupValidationEvent()
        {
            var settings = new Dictionary<string, string?>(CreateEnabledSettings())
            {
                ["EntraSso:AllowedGroupId"] = "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();

            var services = new ServiceCollection();
            var authBuilder = services.AddAuthentication();
            authBuilder.AddEntraSsoTokenValidation(configuration);

            var provider = services.BuildServiceProvider();

            var monitor = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
            var jwtOpts = monitor.Get(EntraSsoDefaults.BearerScheme);

            Assert.NotNull(jwtOpts.Events);
            Assert.NotNull(jwtOpts.Events.OnTokenValidated);
        }

        [Fact]
        public void ConfigureEntraSso_BindsAllowedGroupIdFromConfiguration()
        {
            var settings = new Dictionary<string, string?>(CreateEnabledSettings())
            {
                ["EntraSso:AllowedGroupId"] = "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();

            var services = new ServiceCollection();
            services.ConfigureEntraSso(configuration);

            var provider = services.BuildServiceProvider();

            var opts = provider.GetRequiredService<IOptions<EntraSsoOptions>>().Value;
            Assert.Equal("a1b2c3d4-e5f6-7890-abcd-ef1234567890", opts.AllowedGroupId);
        }

        #endregion
    }
}

using GovUK.Dfe.CoreLibs.Security.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GovUK.Dfe.CoreLibs.Security.Tests.EntraSsoTests
{
    public class EntraSsoOptionsTests
    {
        [Fact]
        public void SectionName_ShouldBeEntraSso()
        {
            Assert.Equal("EntraSso", EntraSsoOptions.SectionName);
        }

        [Fact]
        public void Defaults_ShouldHaveCorrectValues()
        {
            var options = new EntraSsoOptions();

            Assert.False(options.Enabled);
            Assert.Equal("https://login.microsoftonline.com/", options.Instance);
            Assert.Equal(string.Empty, options.TenantId);
            Assert.Equal(string.Empty, options.ClientId);
            Assert.Equal(string.Empty, options.ClientSecret);
            Assert.Equal("/signin-entra", options.CallbackPath);
            Assert.Equal("/signout-callback-entra", options.SignedOutCallbackPath);
            Assert.Equal("code", options.ResponseType);
            Assert.True(options.SaveTokens);
            Assert.True(options.GetClaimsFromUserInfoEndpoint);
            Assert.True(options.RequireHttpsMetadata);
            Assert.True(options.UseTokenLifetime);
            Assert.Equal("preferred_username", options.NameClaimType);
            Assert.Equal(new List<string> { "openid", "profile", "email" }, options.Scopes);
            Assert.Null(options.Audience);
            Assert.Null(options.AllowedGroupId);
        }

        [Fact]
        public void Authority_ShouldBeComputedFromInstanceAndTenantId()
        {
            var options = new EntraSsoOptions
            {
                Instance = "https://login.microsoftonline.com/",
                TenantId = "my-tenant-id"
            };

            Assert.Equal("https://login.microsoftonline.com/my-tenant-id/v2.0", options.Authority);
        }

        [Fact]
        public void Authority_ShouldTrimTrailingSlashFromInstance()
        {
            var options = new EntraSsoOptions
            {
                Instance = "https://login.microsoftonline.com/",
                TenantId = "tenant-abc"
            };

            Assert.Equal("https://login.microsoftonline.com/tenant-abc/v2.0", options.Authority);
            Assert.DoesNotContain("//tenant", options.Authority);
        }

        [Fact]
        public void Authority_ShouldWorkWithCustomInstance()
        {
            var options = new EntraSsoOptions
            {
                Instance = "https://login.microsoftonline.us",
                TenantId = "gov-tenant"
            };

            Assert.Equal("https://login.microsoftonline.us/gov-tenant/v2.0", options.Authority);
        }

        [Fact]
        public void BindFromConfiguration_ShouldPopulateAllProperties()
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
                ["EntraSso:Enabled"] = "true",
                ["EntraSso:Instance"] = "https://login.microsoftonline.com/",
                ["EntraSso:TenantId"] = "9c7d9dd3-840c-4b3f-818e-552865082e16",
                ["EntraSso:ClientId"] = "my-app-client-id",
                ["EntraSso:ClientSecret"] = "super-secret",
                ["EntraSso:CallbackPath"] = "/custom-callback",
                ["EntraSso:SignedOutCallbackPath"] = "/custom-signout",
                ["EntraSso:ResponseType"] = "id_token",
                ["EntraSso:SaveTokens"] = "false",
                ["EntraSso:GetClaimsFromUserInfoEndpoint"] = "false",
                ["EntraSso:RequireHttpsMetadata"] = "false",
                ["EntraSso:UseTokenLifetime"] = "false",
                ["EntraSso:NameClaimType"] = "email",
                ["EntraSso:Scopes:0"] = "openid",
                ["EntraSso:Scopes:1"] = "profile",
                ["EntraSso:Scopes:2"] = "User.Read",
                ["EntraSso:Audience"] = "api://my-app-client-id",
                ["EntraSso:AllowedGroupId"] = "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var services = new ServiceCollection();
            services.Configure<EntraSsoOptions>(configuration.GetSection(EntraSsoOptions.SectionName));
            var provider = services.BuildServiceProvider();

            var opts = provider.GetRequiredService<IOptions<EntraSsoOptions>>().Value;

            Assert.True(opts.Enabled);
            Assert.Equal("https://login.microsoftonline.com/", opts.Instance);
            Assert.Equal("9c7d9dd3-840c-4b3f-818e-552865082e16", opts.TenantId);
            Assert.Equal("my-app-client-id", opts.ClientId);
            Assert.Equal("super-secret", opts.ClientSecret);
            Assert.Equal("/custom-callback", opts.CallbackPath);
            Assert.Equal("/custom-signout", opts.SignedOutCallbackPath);
            Assert.Equal("id_token", opts.ResponseType);
            Assert.False(opts.SaveTokens);
            Assert.False(opts.GetClaimsFromUserInfoEndpoint);
            Assert.False(opts.RequireHttpsMetadata);
            Assert.False(opts.UseTokenLifetime);
            Assert.Equal("email", opts.NameClaimType);
            Assert.Contains("openid", opts.Scopes);
            Assert.Contains("profile", opts.Scopes);
            Assert.Contains("User.Read", opts.Scopes);
            Assert.Equal("api://my-app-client-id", opts.Audience);
            Assert.Equal("a1b2c3d4-e5f6-7890-abcd-ef1234567890", opts.AllowedGroupId);
        }

        [Fact]
        public void BindFromConfiguration_WhenSectionMissing_ShouldUseDefaults()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>())
                .Build();

            var services = new ServiceCollection();
            services.Configure<EntraSsoOptions>(configuration.GetSection(EntraSsoOptions.SectionName));
            var provider = services.BuildServiceProvider();

            var opts = provider.GetRequiredService<IOptions<EntraSsoOptions>>().Value;

            Assert.False(opts.Enabled);
            Assert.Equal("https://login.microsoftonline.com/", opts.Instance);
            Assert.Equal(string.Empty, opts.TenantId);
            Assert.Equal(string.Empty, opts.ClientId);
        }
    }
}

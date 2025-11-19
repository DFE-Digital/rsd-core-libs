using GovUK.Dfe.CoreLibs.Security.Authorization;
using GovUK.Dfe.CoreLibs.Security.Configurations;
using GovUK.Dfe.CoreLibs.Security.Interfaces;
using GovUK.Dfe.CoreLibs.Security.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using Moq;
using NSubstitute;
using System.Text;

namespace GovUK.Dfe.CoreLibs.Security.Tests.AuthorizationTests
{
    public class ServiceCollectionExtensionsTests
    {
        private readonly IConfiguration _configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
        private readonly IServiceCollection _services = new ServiceCollection();

        [Fact]
        public void AddUserTokenService_ShouldRegisterUserTokenService()
        {
            // Act
            _services.AddLogging();
            _services.AddUserTokenService(_configuration);
            var provider = _services.BuildServiceProvider();

            // Assert
            var tokenSettings = provider.GetService<IOptions<TokenSettings>>();
            var userTokenService = provider.GetService<IUserTokenService>();

            Assert.NotNull(tokenSettings);
            Assert.NotNull(userTokenService);
            Assert.IsType<UserTokenService>(userTokenService);
        }

        [Fact]
        public void AddApiOboTokenService_ShouldRegisterApiOboTokenService()
        {
            // Act
            _services.AddMemoryCache();
            _services.AddLogging();
            _services.AddSingleton<IConfiguration>(_configuration);

            var tokenAcquisitionMock = Substitute.For<ITokenAcquisition>();
            _services.AddSingleton(tokenAcquisitionMock);

            _services.AddApiOboTokenService(_configuration);
            var provider = _services.BuildServiceProvider();

            // Assert
            var tokenSettings = provider.GetService<IOptions<TokenSettings>>();
            var apiOboTokenService = provider.GetService<IApiOboTokenService>();

            Assert.NotNull(tokenSettings);
            Assert.NotNull(apiOboTokenService);
            Assert.IsType<ApiOboTokenService>(apiOboTokenService);
        }

        [Fact]
        public void AddCustomJwtAuthentication_ShouldConfigureJwtBearerAuthentication()
        {
            // Arrange
            var authenticationScheme = "CustomJwt";
            var jwtBearerEvents = new Mock<JwtBearerEvents>().Object;

            _services.AddAuthentication()
                     .AddJwtBearer(authenticationScheme, options =>
                     {
                         options.Events = jwtBearerEvents;
                     });

            _services.AddLogging();
            _services.AddSingleton<IConfiguration>(_configuration);

            // Act
            _services.AddCustomJwtAuthentication(
                _configuration,
                authenticationScheme,
                new AuthenticationBuilder(_services),
                jwtBearerEvents);

            var provider = _services.BuildServiceProvider();

            // Assert
            var tokenSettings = _configuration.GetSection("Authorization:TokenSettings").Get<TokenSettings>();
            var symmetricKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenSettings?.SecretKey!));
            var jwtOptionsMonitor = provider.GetService<IOptionsMonitor<JwtBearerOptions>>();

            Assert.NotNull(jwtOptionsMonitor);

            var jwtOptions = jwtOptionsMonitor.Get(authenticationScheme);

            Assert.Equal(tokenSettings?.Issuer, jwtOptions.TokenValidationParameters.ValidIssuer);
            Assert.Equal(tokenSettings?.Audience, jwtOptions.TokenValidationParameters.ValidAudience);
            Assert.Equal(
                symmetricKey.Key,
                ((SymmetricSecurityKey)jwtOptions.TokenValidationParameters.IssuerSigningKey).Key
            );
        }

        [Fact]
        public void AddCustomJwtAuthentication_ShouldThrowException_WhenTokenSettingsMissing()
        {
            // Arrange
            _services.AddMemoryCache();
            _services.AddLogging();

            var invalidConfiguration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>()!)
                .Build();

            var authenticationBuilder = new AuthenticationBuilder(_services);
            var authenticationScheme = "CustomJwt";

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                _services.AddCustomJwtAuthentication(
                    invalidConfiguration,
                    authenticationScheme,
                    authenticationBuilder));
        }

        [Fact]
        public void AddExternalIdentityValidation_ShouldBindOptions_AddHttpClient_AddValidator()
        {
            // Arrange: in-memory config for DfESignIn section
            var inMemorySettings = new Dictionary<string, string>
            {
                ["DfESignIn:Issuer"] = "https://idp.example.com/",
                ["DfESignIn:DiscoveryEndpoint"] = "https://idp.example.com/.well-known/openid-configuration"
            };
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var services = new ServiceCollection();

            // Act
            services.AddExternalIdentityValidation(config);
            var provider = services.BuildServiceProvider();

            // Assert: OpenIdConnectOptions bound
            var opts = provider.GetService<IOptions<OpenIdConnectOptions>>();
            Assert.NotNull(opts);
            Assert.Equal("https://idp.example.com/", opts.Value.Issuer);
            Assert.Equal(
                "https://idp.example.com/.well-known/openid-configuration",
                opts.Value.DiscoveryEndpoint
            );

            // Assert: IHttpClientFactory registered
            var httpClientFactory = provider.GetService<IHttpClientFactory>();
            Assert.NotNull(httpClientFactory);

            // Assert: IExternalIdentityValidator registered
            var validator = provider.GetService<IExternalIdentityValidator>();
            Assert.NotNull(validator);
            Assert.IsType<ExternalIdentityValidator>(validator);
        }

        [Fact]
        public void AddUserTokenServiceFactory_ShouldRegisterFactory()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services.AddUserTokenServiceFactory();
            var provider = services.BuildServiceProvider();

            // Assert
            var factory = provider.GetService<IUserTokenServiceFactory>();
            Assert.NotNull(factory);
            Assert.IsType<UserTokenServiceFactory>(factory);
        }

        [Fact]
        public void AddUserTokenServiceFactory_WithConfigurationSections_ShouldRegisterFactoryAndNamedOptions()
        {
            // Arrange
            var inMemorySettings = new Dictionary<string, string>
            {
                ["Authentication:Primary:SecretKey"] = "primary_secret_key_that_is_long_enough_for_hmacsha256",
                ["Authentication:Primary:Issuer"] = "primary-issuer",
                ["Authentication:Primary:Audience"] = "primary-audience",
                ["Authentication:Primary:TokenLifetimeMinutes"] = "60",
                ["Authentication:Secondary:SecretKey"] = "secondary_secret_key_that_is_long_enough_for_hmacsha256",
                ["Authentication:Secondary:Issuer"] = "secondary-issuer",
                ["Authentication:Secondary:Audience"] = "secondary-audience",
                ["Authentication:Secondary:TokenLifetimeMinutes"] = "30"
            };
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();

            var namedConfigurations = new Dictionary<string, string>
            {
                { "Primary", "Authentication:Primary" },
                { "Secondary", "Authentication:Secondary" }
            };

            // Act
            services.AddUserTokenServiceFactory(config, namedConfigurations);
            var provider = services.BuildServiceProvider();

            // Assert
            var factory = provider.GetService<IUserTokenServiceFactory>();
            Assert.NotNull(factory);

            var optionsMonitor = provider.GetService<IOptionsMonitor<TokenSettings>>();
            Assert.NotNull(optionsMonitor);

            var primaryOptions = optionsMonitor.Get("Primary");
            Assert.Equal("primary_secret_key_that_is_long_enough_for_hmacsha256", primaryOptions.SecretKey);
            Assert.Equal("primary-issuer", primaryOptions.Issuer);
            Assert.Equal("primary-audience", primaryOptions.Audience);
            Assert.Equal(60, primaryOptions.TokenLifetimeMinutes);

            var secondaryOptions = optionsMonitor.Get("Secondary");
            Assert.Equal("secondary_secret_key_that_is_long_enough_for_hmacsha256", secondaryOptions.SecretKey);
            Assert.Equal("secondary-issuer", secondaryOptions.Issuer);
            Assert.Equal("secondary-audience", secondaryOptions.Audience);
            Assert.Equal(30, secondaryOptions.TokenLifetimeMinutes);
        }

        [Fact]
        public void AddUserTokenServiceFactory_WithActionConfiguration_ShouldRegisterFactoryAndNamedOptions()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            var namedConfigurations = new Dictionary<string, Action<TokenSettings>>
            {
                { 
                    "Primary", 
                    settings =>
                    {
                        settings.SecretKey = "primary_secret_key_that_is_long_enough_for_hmacsha256";
                        settings.Issuer = "primary-issuer";
                        settings.Audience = "primary-audience";
                        settings.TokenLifetimeMinutes = 60;
                    }
                },
                { 
                    "Secondary", 
                    settings =>
                    {
                        settings.SecretKey = "secondary_secret_key_that_is_long_enough_for_hmacsha256";
                        settings.Issuer = "secondary-issuer";
                        settings.Audience = "secondary-audience";
                        settings.TokenLifetimeMinutes = 30;
                    }
                }
            };

            // Act
            services.AddUserTokenServiceFactory(namedConfigurations);
            var provider = services.BuildServiceProvider();

            // Assert
            var factory = provider.GetService<IUserTokenServiceFactory>();
            Assert.NotNull(factory);

            var optionsMonitor = provider.GetService<IOptionsMonitor<TokenSettings>>();
            Assert.NotNull(optionsMonitor);

            var primaryOptions = optionsMonitor.Get("Primary");
            Assert.Equal("primary_secret_key_that_is_long_enough_for_hmacsha256", primaryOptions.SecretKey);
            Assert.Equal("primary-issuer", primaryOptions.Issuer);
            Assert.Equal("primary-audience", primaryOptions.Audience);
            Assert.Equal(60, primaryOptions.TokenLifetimeMinutes);

            var secondaryOptions = optionsMonitor.Get("Secondary");
            Assert.Equal("secondary_secret_key_that_is_long_enough_for_hmacsha256", secondaryOptions.SecretKey);
            Assert.Equal("secondary-issuer", secondaryOptions.Issuer);
            Assert.Equal("secondary-audience", secondaryOptions.Audience);
            Assert.Equal(30, secondaryOptions.TokenLifetimeMinutes);
        }

        [Fact]
        public void AddUserTokenServiceFactory_WithMultipleConfigurations_ShouldAllowGettingDifferentServices()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            var namedConfigurations = new Dictionary<string, Action<TokenSettings>>
            {
                { "Config1", s => { s.SecretKey = "config1_secret_key_that_is_long_enough_for_hmacsha256"; s.Issuer = "issuer1"; s.Audience = "audience1"; } },
                { "Config2", s => { s.SecretKey = "config2_secret_key_that_is_long_enough_for_hmacsha256"; s.Issuer = "issuer2"; s.Audience = "audience2"; } },
                { "Config3", s => { s.SecretKey = "config3_secret_key_that_is_long_enough_for_hmacsha256"; s.Issuer = "issuer3"; s.Audience = "audience3"; } }
            };

            services.AddUserTokenServiceFactory(namedConfigurations);
            var provider = services.BuildServiceProvider();

            // Act
            var factory = provider.GetRequiredService<IUserTokenServiceFactory>();
            var service1 = factory.GetService("Config1");
            var service2 = factory.GetService("Config2");
            var service3 = factory.GetService("Config3");

            // Assert
            Assert.NotNull(service1);
            Assert.NotNull(service2);
            Assert.NotNull(service3);
            Assert.NotSame(service1, service2);
            Assert.NotSame(service2, service3);
            Assert.NotSame(service1, service3);
        }

        [Fact]
        public void AddUserTokenServiceFactory_ShouldRegisterHttpContextAccessor()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services.AddUserTokenServiceFactory();
            var provider = services.BuildServiceProvider();

            // Assert
            var httpContextAccessor = provider.GetService<IHttpContextAccessor>();
            Assert.NotNull(httpContextAccessor);
        }

        [Fact]
        public void AddUserTokenServiceFactory_ShouldRegisterAsSingleton()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services.AddUserTokenServiceFactory();
            var provider = services.BuildServiceProvider();

            // Assert - Get factory twice and verify it's the same instance
            var factory1 = provider.GetService<IUserTokenServiceFactory>();
            var factory2 = provider.GetService<IUserTokenServiceFactory>();
            Assert.Same(factory1, factory2);
        }
    }
}

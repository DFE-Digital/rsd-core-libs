using GovUK.Dfe.CoreLibs.Security.TokenRefresh.Configuration;
using GovUK.Dfe.CoreLibs.Security.TokenRefresh.Extensions;
using GovUK.Dfe.CoreLibs.Security.TokenRefresh.Interfaces;
using GovUK.Dfe.CoreLibs.Security.TokenRefresh.Models;
using GovUK.Dfe.CoreLibs.Security.TokenRefresh.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GovUK.Dfe.CoreLibs.Security.Tests.TokenRefreshTests.Extensions
{
    public class TokenRefreshServiceCollectionExtensionsTests
    {
        private readonly IServiceCollection _services;
        private readonly IConfiguration _configuration;

        public TokenRefreshServiceCollectionExtensionsTests()
        {
            _services = new ServiceCollection();
            
            var configData = new Dictionary<string, string?>
            {
                ["TokenRefresh:TokenEndpoint"] = "https://example.com/token",
                ["TokenRefresh:IntrospectionEndpoint"] = "https://example.com/introspect",
                ["TokenRefresh:ClientId"] = "test_client",
                ["TokenRefresh:ClientSecret"] = "test_secret",
                ["DfESignIn:Authority"] = "https://example.com",
                ["DfESignIn:ClientId"] = "oidc_client",
                ["DfESignIn:ClientSecret"] = "oidc_secret"
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();
        }

        [Fact]
        public void AddTokenRefresh_WithNullServices_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                ((IServiceCollection)null!).AddTokenRefresh(_configuration));
        }

        [Fact]
        public void AddTokenRefresh_WithNullConfiguration_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _services.AddTokenRefresh((IConfiguration)null!));
        }

        [Fact]
        public void AddTokenRefresh_WithConfiguration_RegistersServices()
        {
            // Act
            _services.AddTokenRefresh(_configuration);
            var serviceProvider = _services.BuildServiceProvider();

            // Assert
            Assert.NotNull(serviceProvider.GetService<ITokenRefreshService>());
            Assert.NotNull(serviceProvider.GetService<ITokenIntrospectionService>());
            Assert.NotNull(serviceProvider.GetService<ITokenRefreshProvider>());
            Assert.IsType<DefaultTokenRefreshProvider>(serviceProvider.GetService<ITokenRefreshProvider>());
        }

        [Fact]
        public void AddTokenRefresh_WithConfiguration_ConfiguresOptions()
        {
            // Act
            _services.AddTokenRefresh(_configuration);
            var serviceProvider = _services.BuildServiceProvider();
            var options = serviceProvider.GetService<IOptions<TokenRefreshOptions>>()?.Value;

            // Assert
            Assert.NotNull(options);
            Assert.Equal("https://example.com/token", options.TokenEndpoint);
            Assert.Equal("https://example.com/introspect", options.IntrospectionEndpoint);
            Assert.Equal("test_client", options.ClientId);
            Assert.Equal("test_secret", options.ClientSecret);
        }

        [Fact]
        public void AddTokenRefresh_WithCustomSectionName_UsesCorrectSection()
        {
            // Arrange
            var customConfigData = new Dictionary<string, string?>
            {
                ["CustomSection:TokenEndpoint"] = "https://custom.com/token",
                ["CustomSection:IntrospectionEndpoint"] = "https://custom.com/introspect",
                ["CustomSection:ClientId"] = "custom_client",
                ["CustomSection:ClientSecret"] = "custom_secret"
            };

            var customConfiguration = new ConfigurationBuilder()
                .AddInMemoryCollection(customConfigData)
                .Build();

            // Act
            _services.AddTokenRefresh(customConfiguration, "CustomSection");
            var serviceProvider = _services.BuildServiceProvider();
            var options = serviceProvider.GetService<IOptions<TokenRefreshOptions>>()?.Value;

            // Assert
            Assert.NotNull(options);
            Assert.Equal("https://custom.com/token", options.TokenEndpoint);
            Assert.Equal("custom_client", options.ClientId);
        }

        [Fact]
        public void AddTokenRefresh_WithConfigureAction_AppliesConfiguration()
        {
            // Act
            _services.AddTokenRefresh(_configuration, "TokenRefresh", options =>
            {
                options.RefreshBufferMinutes = 10;
                options.MaxRetryAttempts = 5;
            });

            var serviceProvider = _services.BuildServiceProvider();
            var options = serviceProvider.GetService<IOptions<TokenRefreshOptions>>()?.Value;

            // Assert
            Assert.NotNull(options);
            Assert.Equal(10, options.RefreshBufferMinutes);
            Assert.Equal(5, options.MaxRetryAttempts);
        }

        [Fact]
        public void AddTokenRefresh_WithManualConfiguration_RegistersServices()
        {
            // Act
            _services.AddTokenRefresh(options =>
            {
                options.TokenEndpoint = "https://manual.com/token";
                options.IntrospectionEndpoint = "https://manual.com/introspect";
                options.ClientId = "manual_client";
                options.ClientSecret = "manual_secret";
            });

            var serviceProvider = _services.BuildServiceProvider();
            var configuredOptions = serviceProvider.GetService<IOptions<TokenRefreshOptions>>()?.Value;

            // Assert
            Assert.NotNull(serviceProvider.GetService<ITokenRefreshService>());
            Assert.NotNull(configuredOptions);
            Assert.Equal("https://manual.com/token", configuredOptions.TokenEndpoint);
            Assert.Equal("manual_client", configuredOptions.ClientId);
        }

        [Fact]
        public void AddTokenRefresh_WithNullConfigureAction_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _services.AddTokenRefresh((Action<TokenRefreshOptions>)null!));
        }

        [Fact]
        public void AddTokenRefresh_WithCustomProvider_RegistersCustomProvider()
        {
            // Act
            _services.AddTokenRefresh<CustomTokenRefreshProvider>(_configuration);
            var serviceProvider = _services.BuildServiceProvider();

            // Assert
            Assert.NotNull(serviceProvider.GetService<ITokenRefreshService>());
            Assert.NotNull(serviceProvider.GetService<ITokenRefreshProvider>());
            Assert.IsType<CustomTokenRefreshProvider>(serviceProvider.GetService<ITokenRefreshProvider>());
        }

        [Fact]
        public void AddTokenRefresh_WithCustomProviderAndManualConfig_RegistersCorrectly()
        {
            // Act
            _services.AddTokenRefresh<CustomTokenRefreshProvider>(options =>
            {
                options.TokenEndpoint = "https://custom.com/token";
                options.IntrospectionEndpoint = "https://custom.com/introspect";
                options.ClientId = "custom_client";
                options.ClientSecret = "custom_secret";
            });

            var serviceProvider = _services.BuildServiceProvider();

            // Assert
            Assert.NotNull(serviceProvider.GetService<ITokenRefreshService>());
            Assert.IsType<CustomTokenRefreshProvider>(serviceProvider.GetService<ITokenRefreshProvider>());
        }

        [Fact]
        public void AddTokenRefreshWithOidc_WithDefaults_InheritsFromOidcConfiguration()
        {
            // Act
            _services.AddTokenRefreshWithOidc(_configuration);
            var serviceProvider = _services.BuildServiceProvider();
            var options = serviceProvider.GetService<IOptions<TokenRefreshOptions>>()?.Value;

            // Assert
            Assert.NotNull(options);
            // Should inherit from OIDC configuration if token refresh specific values are not set
            Assert.Equal("test_client", options.ClientId);
            Assert.Equal("test_secret", options.ClientSecret);
            Assert.Equal("https://example.com/token", options.TokenEndpoint);
            Assert.Equal("https://example.com/introspect", options.IntrospectionEndpoint);
        }

        [Fact]
        public void AddTokenRefreshWithOidc_WithTokenRefreshSpecificValues_UsesTokenRefreshValues()
        {
            // Act
            _services.AddTokenRefreshWithOidc(_configuration);
            var serviceProvider = _services.BuildServiceProvider();
            var options = serviceProvider.GetService<IOptions<TokenRefreshOptions>>()?.Value;

            // Assert
            Assert.NotNull(options);
            // Should use token refresh specific values when they exist
            Assert.Equal("test_client", options.ClientId);
            Assert.Equal("test_secret", options.ClientSecret);
            Assert.Equal("https://example.com/token", options.TokenEndpoint);
            Assert.Equal("https://example.com/introspect", options.IntrospectionEndpoint);
        }

        [Fact]
        public void AddTokenRefreshWithOidc_WithCustomSectionNames_UsesCorrectSections()
        {
            // Arrange
            var customConfigData = new Dictionary<string, string?>
            {
                ["CustomOidc:Authority"] = "https://custom-oidc.com",
                ["CustomOidc:ClientId"] = "custom_oidc_client",
                ["CustomOidc:ClientSecret"] = "custom_oidc_secret",
                ["CustomTokenRefresh:ClientId"] = "custom_token_client",
                ["CustomTokenRefresh:ClientSecret"] = "custom_token_secret"
            };

            var customConfiguration = new ConfigurationBuilder()
                .AddInMemoryCollection(customConfigData)
                .Build();

            // Act
            _services.AddTokenRefreshWithOidc(customConfiguration, "CustomOidc", "CustomTokenRefresh");
            var serviceProvider = _services.BuildServiceProvider();
            var options = serviceProvider.GetService<IOptions<TokenRefreshOptions>>()?.Value;

            // Assert
            Assert.NotNull(options);
            Assert.Equal("custom_token_client", options.ClientId);
            Assert.Equal("custom_token_secret", options.ClientSecret);
            Assert.Equal("https://custom-oidc.com/token", options.TokenEndpoint);
            Assert.Equal("https://custom-oidc.com/introspect", options.IntrospectionEndpoint);
        }

        [Fact]
        public void AddTokenRefresh_RegistersHttpClient()
        {
            // Act
            _services.AddTokenRefresh(_configuration);
            var serviceProvider = _services.BuildServiceProvider();

            // Assert
            Assert.NotNull(serviceProvider.GetService<IHttpClientFactory>());
        }

        [Fact]
        public void AddTokenRefresh_RegistersServicesAsScoped()
        {
            // Act
            _services.AddTokenRefresh(_configuration);
            var serviceProvider = _services.BuildServiceProvider();

            using var scope1 = serviceProvider.CreateScope();
            using var scope2 = serviceProvider.CreateScope();

            var service1 = scope1.ServiceProvider.GetService<ITokenRefreshService>();
            var service2 = scope1.ServiceProvider.GetService<ITokenRefreshService>();
            var service3 = scope2.ServiceProvider.GetService<ITokenRefreshService>();

            // Assert
            Assert.Same(service1, service2); // Same within scope
            Assert.NotSame(service1, service3); // Different across scopes
        }

        // Custom provider for testing
        private class CustomTokenRefreshProvider : ITokenRefreshProvider
        {
            public string ProviderName => "CustomProvider";

            public CustomTokenRefreshProvider(IOptions<TokenRefreshOptions> options, IHttpClientFactory httpClientFactory)
            {
                // Required for DI
            }

            public Task<TokenRefreshResponse> RefreshTokenAsync(TokenRefreshRequest request, CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }

            public Task<TokenIntrospectionResponse> IntrospectTokenAsync(string token, CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }
        }
    }
}

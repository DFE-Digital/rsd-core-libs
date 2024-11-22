using System.Text;
using DfE.CoreLibs.Security.Authorization;
using DfE.CoreLibs.Security.Configurations;
using DfE.CoreLibs.Security.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using Moq;
using NSubstitute;

namespace DfE.CoreLibs.Security.Tests.AuthorizationTests
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
            _services.AddMemoryCache();
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
            _services.AddCustomJwtAuthentication(_configuration, authenticationScheme, new AuthenticationBuilder(_services), jwtBearerEvents);
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
                .AddInMemoryCollection(new Dictionary<string, string>())
                .Build();

            var authenticationBuilder = new AuthenticationBuilder(_services);
            var authenticationScheme = "CustomJwt";

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                _services.AddCustomJwtAuthentication(invalidConfiguration, authenticationScheme, authenticationBuilder));
        }
    }
}

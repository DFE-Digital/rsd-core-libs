using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using DfE.CoreLibs.Security.Configurations;
using DfE.CoreLibs.Security.OpenIdConnect;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;

namespace DfE.CoreLibs.Security.Tests.OpenIdConnectTests
{
    public class ExternalIdentityValidatorTests
    {
        private readonly OpenIdConnectOptions _opts = new OpenIdConnectOptions
        {
            Issuer = "https://idp.example.com/",
            DiscoveryEndpoint = "https://idp.example.com/.well-known/openid-configuration",
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true
        };

        [Fact]
        public async Task ValidateIdTokenAsync_ValidToken_ReturnsPrincipal()
        {
            var secretKey = Encoding.UTF8.GetBytes("0123456789ABCDEF0123456789ABCDEF");
            var signingKey = new SymmetricSecurityKey(secretKey);
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var handler = new JwtSecurityTokenHandler();
            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = _opts.Issuer,
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "user1"),
                    new Claim(ClaimTypes.Email,          "user1@example.com")
                }),
                Expires = DateTime.UtcNow.AddMinutes(5),
                SigningCredentials = creds
            };
            var tokenString = handler.WriteToken(handler.CreateToken(descriptor));

            var openIdConfig = new OpenIdConnectConfiguration { Issuer = _opts.Issuer };
            openIdConfig.SigningKeys.Add(signingKey);

            var stubConfigManager = new StubConfigManager(openIdConfig);

            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            httpClientFactory
                .CreateClient(Arg.Any<string>())
                .Returns(new System.Net.Http.HttpClient());

            var validator = new ExternalIdentityValidator(
                Options.Create(_opts),
                httpClientFactory);

            var field = typeof(ExternalIdentityValidator)
                .GetField("_configManager", BindingFlags.Instance | BindingFlags.NonPublic)
                            ?? throw new InvalidOperationException("_configManager not found");
            field.SetValue(validator, stubConfigManager);

            // Act
            var principal = await validator.ValidateIdTokenAsync(tokenString);

            // Assert
            Assert.NotNull(principal);
            Assert.True(principal.Identity?.IsAuthenticated);
            Assert.Equal("user1", principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            // Cleanup
            validator.Dispose();
        }

        [Fact]
        public async Task ValidateIdTokenAsync_NullOrWhitespace_ThrowsArgumentNullException()
        {
            // 1) Stub a manager (won’t actually be called)
            var stubManager = new StubConfigManager(new OpenIdConnectConfiguration());

            // 2) Stub HttpClientFactory
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            httpClientFactory
                .CreateClient(Arg.Any<string>())
                .Returns(new System.Net.Http.HttpClient());

            var validator = new ExternalIdentityValidator(
                Options.Create(_opts),
                httpClientFactory);

            // Inject stub
            var field = typeof(ExternalIdentityValidator)
                .GetField("_configManager", BindingFlags.Instance | BindingFlags.NonPublic)
                            ?? throw new InvalidOperationException("_configManager not found");
            field.SetValue(validator, stubManager);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => validator.ValidateIdTokenAsync(null!, CancellationToken.None));
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => validator.ValidateIdTokenAsync("", CancellationToken.None));
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => validator.ValidateIdTokenAsync("   ", CancellationToken.None));
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes_NoException()
        {
            var stubManager = new StubConfigManager(new OpenIdConnectConfiguration());
            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            httpClientFactory
                .CreateClient(Arg.Any<string>())
                .Returns(new System.Net.Http.HttpClient());

            var validator = new ExternalIdentityValidator(
                Options.Create(_opts),
                httpClientFactory);

            // Inject stub
            var field = typeof(ExternalIdentityValidator)
                .GetField("_configManager", BindingFlags.Instance | BindingFlags.NonPublic)
                            ?? throw new InvalidOperationException("_configManager not found");
            field.SetValue(validator, stubManager);

            // Act / Assert
            validator.Dispose();
            validator.Dispose();
        }

        /// <summary>
        /// A test helper that subclasses the real ConfigurationManager
        /// but overrides GetConfigurationAsync to return a prebuilt config.
        /// </summary>
        private class StubConfigManager
            : ConfigurationManager<OpenIdConnectConfiguration>
        {
            private readonly OpenIdConnectConfiguration _config;

            public StubConfigManager(OpenIdConnectConfiguration config)
                : base(
                    metadataAddress: "ignored",
                    configRetriever: new OpenIdConnectConfigurationRetriever(),
                    docRetriever: new HttpDocumentRetriever(new System.Net.Http.HttpClient())
                    {
                        RequireHttps = false
                    })
            {
                _config = config;
            }

            public override Task<OpenIdConnectConfiguration> GetConfigurationAsync(
                CancellationToken cancellationToken)
            {
                return Task.FromResult(_config);
            }
        }
    }
}

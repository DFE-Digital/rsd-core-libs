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
        private readonly OpenIdConnectOptions _oidcOpts = new OpenIdConnectOptions
        {
            Issuer = "https://idp.example.com/",
            DiscoveryEndpoint = "https://idp.example.com/.well-known/openid-configuration",
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true
        };

        private static IHttpClientFactory CreateHttpClientFactory()
        {
            var factory = Substitute.For<IHttpClientFactory>();
            // Always return a real HttpClient so HttpDocumentRetriever ctor won't get null
            factory.CreateClient(Arg.Any<string>()).Returns(new System.Net.Http.HttpClient());
            return factory;
        }


        [Fact]
        public async Task ValidateIdTokenAsync_ValidToken_ReturnsPrincipal()
        {
            var secretKey = Encoding.UTF8.GetBytes("0123456789ABCDEF0123456789ABCDEF");
            var signingKey = new SymmetricSecurityKey(secretKey);
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var handler = new JwtSecurityTokenHandler();
            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = _oidcOpts.Issuer,
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "user1"),
                    new Claim(ClaimTypes.Email,          "user1@example.com")
                }),
                Expires = DateTime.UtcNow.AddMinutes(5),
                SigningCredentials = creds
            };
            var tokenString = handler.WriteToken(handler.CreateToken(descriptor));

            var openIdConfig = new OpenIdConnectConfiguration { Issuer = _oidcOpts.Issuer };
            openIdConfig.SigningKeys.Add(signingKey);

            var stubConfigManager = new StubConfigManager(openIdConfig);

            var httpClientFactory = Substitute.For<IHttpClientFactory>();
            httpClientFactory
                .CreateClient(Arg.Any<string>())
                .Returns(new System.Net.Http.HttpClient());

            var validator = new ExternalIdentityValidator(
                Options.Create(_oidcOpts),
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
                Options.Create(_oidcOpts),
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
                Options.Create(_oidcOpts),
                httpClientFactory);

            // Inject stub
            var field = typeof(ExternalIdentityValidator)
                .GetField("_configManager", BindingFlags.Instance | BindingFlags.NonPublic)
                            ?? throw new InvalidOperationException("_configManager not found");
            field.SetValue(validator, stubManager);

            // Act / Assert
            validator.Dispose();
            validator.Dispose();

            Assert.True(true);
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

        [Fact]
        public void Constructor_NullOptions_ThrowsArgumentNullException()
        {
            var factory = CreateHttpClientFactory();
            Assert.Throws<ArgumentNullException>(() =>
                new ExternalIdentityValidator(
                    /* options: */ null!,
                    factory));
        }

        [Fact]
        public void IsTestAuthenticationEnabled_Default_ReturnsFalse()
        {
            var factory = CreateHttpClientFactory();
            var validator = new ExternalIdentityValidator(
                Options.Create(_oidcOpts),
                factory);

            Assert.False(validator.IsTestAuthenticationEnabled);
        }

        [Fact]
        public void IsTestAuthenticationEnabled_WhenEnabled_ReturnsTrue()
        {
            var factory = CreateHttpClientFactory();
            var testOpts = new TestAuthenticationOptions
            {
                Enabled = true,
                JwtSigningKey = "any-key",
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false
            };

            var validator = new ExternalIdentityValidator(
                Options.Create(_oidcOpts),
                factory,
                Options.Create(testOpts));

            Assert.True(validator.IsTestAuthenticationEnabled);
        }

        [Fact]
        public void ValidateTestIdToken_NullOrWhitespace_ThrowsArgumentNullException()
        {
            var factory = CreateHttpClientFactory();
            var testOpts = new TestAuthenticationOptions
            {
                Enabled = true,
                JwtSigningKey = "key",
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false
            };
            var validator = new ExternalIdentityValidator(
                Options.Create(_oidcOpts),
                factory,
                Options.Create(testOpts));

            Assert.Throws<ArgumentNullException>(() => validator.ValidateTestIdToken(null!));
            Assert.Throws<ArgumentNullException>(() => validator.ValidateTestIdToken(""));
            Assert.Throws<ArgumentNullException>(() => validator.ValidateTestIdToken("   "));
        }

        [Fact]
        public void ValidateTestIdToken_NotEnabled_ThrowsInvalidOperationException()
        {
            var factory = CreateHttpClientFactory();
            var testOpts = new TestAuthenticationOptions
            {
                Enabled = false,
                JwtSigningKey = "key",
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false
            };
            var validator = new ExternalIdentityValidator(
                Options.Create(_oidcOpts),
                factory,
                Options.Create(testOpts));

            Assert.Throws<InvalidOperationException>(() =>
                validator.ValidateTestIdToken("some-token"));
        }

        [Fact]
        public void ValidateTestIdToken_NoSigningKey_ThrowsInvalidOperationException()
        {
            var factory = CreateHttpClientFactory();
            var testOpts = new TestAuthenticationOptions
            {
                Enabled = true,
                JwtSigningKey = "", // missing
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false
            };
            var validator = new ExternalIdentityValidator(
                Options.Create(_oidcOpts),
                factory,
                Options.Create(testOpts));

            Assert.Throws<InvalidOperationException>(() =>
                validator.ValidateTestIdToken("some-token"));
        }

        [Fact]
        public void ValidateTestIdToken_InvalidSignature_ThrowsSecurityTokenException()
        {
            var factory = CreateHttpClientFactory();
            var validKey = Encoding.UTF8.GetBytes("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
            var testOpts = new TestAuthenticationOptions
            {
                Enabled = true,
                JwtSigningKey = Encoding.UTF8.GetString(validKey),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false
            };
            var validator = new ExternalIdentityValidator(
                Options.Create(_oidcOpts),
                factory,
                Options.Create(testOpts));

            // Token signed with a *different* key
            var badKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB"));
            var creds = new SigningCredentials(badKey, SecurityAlgorithms.HmacSha256);
            var handler = new JwtSecurityTokenHandler();
            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "user-x")
                }),
                Expires = DateTime.UtcNow.AddMinutes(5),
                SigningCredentials = creds
            };
            var badToken = handler.WriteToken(handler.CreateToken(descriptor));

            Assert.Throws<SecurityTokenException>(() =>
                validator.ValidateTestIdToken(badToken));
        }

        [Fact]
        public void ValidateTestIdToken_ValidToken_ReturnsPrincipal()
        {
            var factory = CreateHttpClientFactory();
            var rawKey = "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC";
            var testOpts = new TestAuthenticationOptions
            {
                Enabled = true,
                JwtSigningKey = rawKey,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false
            };
            var validator = new ExternalIdentityValidator(
                Options.Create(_oidcOpts),
                factory,
                Options.Create(testOpts));

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(rawKey));
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            var handler = new JwtSecurityTokenHandler();
            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "test-user"),
                    new Claim(ClaimTypes.Email, "test@example.com")
                }),
                Expires = DateTime.UtcNow.AddMinutes(5),
                SigningCredentials = creds
            };
            var token = handler.WriteToken(handler.CreateToken(descriptor));

            var principal = validator.ValidateTestIdToken(token);

            Assert.NotNull(principal);
            Assert.True(principal.Identity?.IsAuthenticated);
            Assert.Equal("test-user",
                principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            Assert.Equal("test@example.com",
                principal.FindFirst(ClaimTypes.Email)?.Value);
        }

        [Fact]
        public async System.Threading.Tasks.Task ValidateIdTokenAsync_WithTestOptionsEnabled_UsesTestPath()
        {
            var factory = CreateHttpClientFactory();
            var rawKey = "DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD";
            var testOpts = new TestAuthenticationOptions
            {
                Enabled = true,
                JwtSigningKey = rawKey,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false
            };
            var validator = new ExternalIdentityValidator(
                Options.Create(_oidcOpts),
                factory,
                Options.Create(testOpts));

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(rawKey));
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            var handler = new JwtSecurityTokenHandler();
            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "async-user")
                }),
                Expires = DateTime.UtcNow.AddMinutes(5),
                SigningCredentials = creds
            };
            var token = handler.WriteToken(handler.CreateToken(descriptor));

            var principal = await validator.ValidateIdTokenAsync(token);

            Assert.NotNull(principal);
            Assert.True(principal.Identity?.IsAuthenticated);
            Assert.Equal("async-user",
                principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        }
    }
}

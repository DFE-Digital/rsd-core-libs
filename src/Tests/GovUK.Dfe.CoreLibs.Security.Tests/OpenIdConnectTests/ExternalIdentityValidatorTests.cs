using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using GovUK.Dfe.CoreLibs.Security.Configurations;
using GovUK.Dfe.CoreLibs.Security.OpenIdConnect;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;

namespace GovUK.Dfe.CoreLibs.Security.Tests.OpenIdConnectTests
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
                () => validator.ValidateIdTokenAsync(null!, false,cancellationToken:CancellationToken.None));
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => validator.ValidateIdTokenAsync("", false, cancellationToken: CancellationToken.None));
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => validator.ValidateIdTokenAsync("   ", false, cancellationToken: CancellationToken.None));
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
                testOptions:Options.Create(testOpts));

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
                testOptions: Options.Create(testOpts));

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
                testOptions: Options.Create(testOpts));

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
                testOptions: Options.Create(testOpts));

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
                testOptions: Options.Create(testOpts));

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
                validator.ValidateTestIdToken(badToken, true));
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
                testOptions: Options.Create(testOpts));

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

            var principal = validator.ValidateTestIdToken(token, true);

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
                testOptions: Options.Create(testOpts));

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

            var principal = await validator.ValidateIdTokenAsync(token, true);

            Assert.NotNull(principal);
            Assert.True(principal.Identity?.IsAuthenticated);
            Assert.Equal("async-user",
                principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        }

        #region ValidateInternalAuthToken Tests

        [Fact]
        public void ValidateInternalAuthToken_NullOrWhitespace_ThrowsArgumentNullException()
        {
            var factory = CreateHttpClientFactory();
            var internalAuthOpts = new InternalServiceAuthOptions
            {
                SecretKey = "internal_auth_key_that_is_long_enough_32",
                Issuer = "internal-issuer",
                Audience = "internal-audience"
            };
            var validator = new ExternalIdentityValidator(
                Options.Create(_oidcOpts),
                factory,
                internalAuthOpts: Options.Create(internalAuthOpts));

            Assert.Throws<ArgumentNullException>(() => validator.ValidateInternalAuthToken(null!));
            Assert.Throws<ArgumentNullException>(() => validator.ValidateInternalAuthToken(""));
            Assert.Throws<ArgumentNullException>(() => validator.ValidateInternalAuthToken("   "));
        }

        [Fact]
        public void ValidateInternalAuthToken_NoSecretKey_ThrowsInvalidOperationException()
        {
            var factory = CreateHttpClientFactory();
            var internalAuthOpts = new InternalServiceAuthOptions
            {
                SecretKey = "", // missing
                Issuer = "internal-issuer",
                Audience = "internal-audience"
            };
            var validator = new ExternalIdentityValidator(
                Options.Create(_oidcOpts),
                factory,
                internalAuthOpts: Options.Create(internalAuthOpts));

            Assert.Throws<InvalidOperationException>(() =>
                validator.ValidateInternalAuthToken("some-token"));
        }

        [Fact]
        public void ValidateInternalAuthToken_NullInternalAuthOptions_ThrowsInvalidOperationException()
        {
            var factory = CreateHttpClientFactory();
            var validator = new ExternalIdentityValidator(
                Options.Create(_oidcOpts),
                factory,
                internalAuthOpts: null);

            Assert.Throws<InvalidOperationException>(() =>
                validator.ValidateInternalAuthToken("some-token"));
        }

        [Fact]
        public void ValidateInternalAuthToken_InvalidSignature_ThrowsSecurityTokenException()
        {
            var factory = CreateHttpClientFactory();
            var validKey = Encoding.UTF8.GetBytes("EEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE");
            var internalAuthOpts = new InternalServiceAuthOptions
            {
                SecretKey = Encoding.UTF8.GetString(validKey),
                Issuer = "internal-issuer",
                Audience = "internal-audience"
            };
            var validator = new ExternalIdentityValidator(
                Options.Create(_oidcOpts),
                factory,
                internalAuthOpts: Options.Create(internalAuthOpts));

            // Token signed with a *different* key
            var badKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF"));
            var creds = new SigningCredentials(badKey, SecurityAlgorithms.HmacSha256);
            var handler = new JwtSecurityTokenHandler();
            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = "internal-issuer",
                Audience = "internal-audience",
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "internal-user")
                }),
                Expires = DateTime.UtcNow.AddMinutes(5),
                SigningCredentials = creds
            };
            var badToken = handler.WriteToken(handler.CreateToken(descriptor));

            var exception = Assert.Throws<SecurityTokenException>(() =>
                validator.ValidateInternalAuthToken(badToken));
            Assert.Contains("Internal Auth token validation failed", exception.Message);
        }

        [Fact]
        public void ValidateInternalAuthToken_InvalidIssuer_ThrowsSecurityTokenException()
        {
            var factory = CreateHttpClientFactory();
            var rawKey = "GGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG";
            var internalAuthOpts = new InternalServiceAuthOptions
            {
                SecretKey = rawKey,
                Issuer = "expected-issuer",
                Audience = "internal-audience"
            };
            var validator = new ExternalIdentityValidator(
                Options.Create(_oidcOpts),
                factory,
                internalAuthOpts: Options.Create(internalAuthOpts));

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(rawKey));
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            var handler = new JwtSecurityTokenHandler();
            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = "wrong-issuer", // Different issuer
                Audience = "internal-audience",
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "internal-user")
                }),
                Expires = DateTime.UtcNow.AddMinutes(5),
                SigningCredentials = creds
            };
            var token = handler.WriteToken(handler.CreateToken(descriptor));

            var exception = Assert.Throws<SecurityTokenException>(() =>
                validator.ValidateInternalAuthToken(token));
            Assert.Contains("Internal Auth token validation failed", exception.Message);
        }

        [Fact]
        public void ValidateInternalAuthToken_InvalidAudience_ThrowsSecurityTokenException()
        {
            var factory = CreateHttpClientFactory();
            var rawKey = "HHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHH";
            var internalAuthOpts = new InternalServiceAuthOptions
            {
                SecretKey = rawKey,
                Issuer = "internal-issuer",
                Audience = "expected-audience"
            };
            var validator = new ExternalIdentityValidator(
                Options.Create(_oidcOpts),
                factory,
                internalAuthOpts: Options.Create(internalAuthOpts));

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(rawKey));
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            var handler = new JwtSecurityTokenHandler();
            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = "internal-issuer",
                Audience = "wrong-audience", // Different audience
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "internal-user")
                }),
                Expires = DateTime.UtcNow.AddMinutes(5),
                SigningCredentials = creds
            };
            var token = handler.WriteToken(handler.CreateToken(descriptor));

            var exception = Assert.Throws<SecurityTokenException>(() =>
                validator.ValidateInternalAuthToken(token));
            Assert.Contains("Internal Auth token validation failed", exception.Message);
        }

        [Fact]
        public void ValidateInternalAuthToken_ValidToken_ReturnsPrincipal()
        {
            var factory = CreateHttpClientFactory();
            var rawKey = "IIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII";
            var internalAuthOpts = new InternalServiceAuthOptions
            {
                SecretKey = rawKey,
                Issuer = "internal-issuer",
                Audience = "internal-audience"
            };
            var validator = new ExternalIdentityValidator(
                Options.Create(_oidcOpts),
                factory,
                internalAuthOpts: Options.Create(internalAuthOpts));

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(rawKey));
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            var handler = new JwtSecurityTokenHandler();
            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = "internal-issuer",
                Audience = "internal-audience",
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "internal-user"),
                    new Claim(ClaimTypes.Email, "internal@example.com"),
                    new Claim(ClaimTypes.Role, "InternalService")
                }),
                Expires = DateTime.UtcNow.AddMinutes(5),
                SigningCredentials = creds
            };
            var token = handler.WriteToken(handler.CreateToken(descriptor));

            var principal = validator.ValidateInternalAuthToken(token);

            Assert.NotNull(principal);
            Assert.True(principal.Identity?.IsAuthenticated);
            Assert.Equal("internal-user",
                principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            Assert.Equal("internal@example.com",
                principal.FindFirst(ClaimTypes.Email)?.Value);
            Assert.Equal("InternalService",
                principal.FindFirst(ClaimTypes.Role)?.Value);
        }

        [Fact]
        public void ValidateInternalAuthToken_ValidTokenWithMultipleClaims_ReturnsAllClaims()
        {
            var factory = CreateHttpClientFactory();
            var rawKey = "JJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJ";
            var internalAuthOpts = new InternalServiceAuthOptions
            {
                SecretKey = rawKey,
                Issuer = "internal-issuer",
                Audience = "internal-audience"
            };
            var validator = new ExternalIdentityValidator(
                Options.Create(_oidcOpts),
                factory,
                internalAuthOpts: Options.Create(internalAuthOpts));

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(rawKey));
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            var handler = new JwtSecurityTokenHandler();
            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = "internal-issuer",
                Audience = "internal-audience",
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "service-123"),
                    new Claim(ClaimTypes.Name, "InternalService"),
                    new Claim(ClaimTypes.Email, "service@internal.com"),
                    new Claim(ClaimTypes.Role, "ServiceAccount"),
                    new Claim("custom-claim", "custom-value")
                }),
                Expires = DateTime.UtcNow.AddMinutes(5),
                SigningCredentials = creds
            };
            var token = handler.WriteToken(handler.CreateToken(descriptor));

            var principal = validator.ValidateInternalAuthToken(token);

            Assert.NotNull(principal);
            Assert.True(principal.Identity?.IsAuthenticated);
            Assert.Equal("service-123", principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            Assert.Equal("InternalService", principal.FindFirst(ClaimTypes.Name)?.Value);
            Assert.Equal("service@internal.com", principal.FindFirst(ClaimTypes.Email)?.Value);
            Assert.Equal("ServiceAccount", principal.FindFirst(ClaimTypes.Role)?.Value);
            Assert.Equal("custom-value", principal.FindFirst("custom-claim")?.Value);
        }

        [Fact]
        public async Task ValidateIdTokenAsync_WithValidInternalRequest_UsesInternalAuthPath()
        {
            var factory = CreateHttpClientFactory();
            var rawKey = "KKKKKKKKKKKKKKKKKKKKKKKKKKKKKKKK";
            var internalAuthOpts = new InternalServiceAuthOptions
            {
                SecretKey = rawKey,
                Issuer = "internal-issuer",
                Audience = "internal-audience"
            };
            var validator = new ExternalIdentityValidator(
                Options.Create(_oidcOpts),
                factory,
                internalAuthOpts: Options.Create(internalAuthOpts));

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(rawKey));
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            var handler = new JwtSecurityTokenHandler();
            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = "internal-issuer",
                Audience = "internal-audience",
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "async-internal-user")
                }),
                Expires = DateTime.UtcNow.AddMinutes(5),
                SigningCredentials = creds
            };
            var token = handler.WriteToken(handler.CreateToken(descriptor));

            // Act - with validInternalRequest = true
            var principal = await validator.ValidateIdTokenAsync(token, validCypressRequest: false, validInternalRequest: true);

            Assert.NotNull(principal);
            Assert.True(principal.Identity?.IsAuthenticated);
            Assert.Equal("async-internal-user",
                principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        }

        [Fact]
        public async Task ValidateIdTokenAsync_WithInternalAuthConfigured_ButValidInternalRequestFalse_DoesNotUseInternalPath()
        {
            var factory = CreateHttpClientFactory();
            var rawKey = "LLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLL";
            var internalAuthOpts = new InternalServiceAuthOptions
            {
                SecretKey = rawKey,
                Issuer = "internal-issuer",
                Audience = "internal-audience"
            };

            // Create a validator with internal auth configured
            var validator = new ExternalIdentityValidator(
                Options.Create(_oidcOpts),
                factory,
                internalAuthOpts: Options.Create(internalAuthOpts));

            // Create a token signed with internal auth key but wrong issuer for OIDC
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(rawKey));
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            var handler = new JwtSecurityTokenHandler();
            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = "internal-issuer", // This won't match OIDC issuer
                Audience = "internal-audience",
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "user")
                }),
                Expires = DateTime.UtcNow.AddMinutes(5),
                SigningCredentials = creds
            };
            var token = handler.WriteToken(handler.CreateToken(descriptor));

            // Act & Assert - with validInternalRequest = false, it should try OIDC path and fail
            await Assert.ThrowsAnyAsync<Exception>(() =>
                validator.ValidateIdTokenAsync(token, validCypressRequest: false, validInternalRequest: false));
        }

        [Fact]
        public void ValidateInternalAuthToken_ExpiredToken_ThrowsSecurityTokenException()
        {
            var factory = CreateHttpClientFactory();
            var rawKey = "MMMMMMMMMMMMMMMMMMMMMMMMMMMMMMMM";
            var internalAuthOpts = new InternalServiceAuthOptions
            {
                SecretKey = rawKey,
                Issuer = "internal-issuer",
                Audience = "internal-audience"
            };
            var validator = new ExternalIdentityValidator(
                Options.Create(_oidcOpts),
                factory,
                internalAuthOpts: Options.Create(internalAuthOpts));

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(rawKey));
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            var handler = new JwtSecurityTokenHandler();
            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = "internal-issuer",
                Audience = "internal-audience",
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "internal-user")
                }),
                NotBefore = DateTime.UtcNow.AddMinutes(-15), // Token valid from 15 minutes ago
                Expires = DateTime.UtcNow.AddMinutes(-10), // Expired 10 minutes ago
                SigningCredentials = creds
            };
            var token = handler.WriteToken(handler.CreateToken(descriptor));

            var exception = Assert.Throws<SecurityTokenException>(() =>
                validator.ValidateInternalAuthToken(token));
            Assert.Contains("Internal Auth token validation failed", exception.Message);
        }

        [Fact]
        public void ValidateInternalAuthToken_MalformedToken_ThrowsSecurityTokenException()
        {
            var factory = CreateHttpClientFactory();
            var rawKey = "NNNNNNNNNNNNNNNNNNNNNNNNNNNNNNNN";
            var internalAuthOpts = new InternalServiceAuthOptions
            {
                SecretKey = rawKey,
                Issuer = "internal-issuer",
                Audience = "internal-audience"
            };
            var validator = new ExternalIdentityValidator(
                Options.Create(_oidcOpts),
                factory,
                internalAuthOpts: Options.Create(internalAuthOpts));

            var malformedToken = "this.is.not.a.valid.jwt.token";

            var exception = Assert.Throws<SecurityTokenException>(() =>
                validator.ValidateInternalAuthToken(malformedToken));
            Assert.Contains("Internal Auth token validation failed", exception.Message);
        }

        #endregion
    }
}

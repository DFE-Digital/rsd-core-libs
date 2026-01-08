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
            factory.CreateClient(Arg.Any<string>()).Returns(new System.Net.Http.HttpClient());
            return factory;
        }

        /// <summary>
        /// Helper to inject stub provider configurations into the validator via reflection.
        /// Works with the new ProviderConfiguration-based structure.
        /// </summary>
        private static void InjectProviders(
            ExternalIdentityValidator validator,
            params (OpenIdConnectOptions opts, StubConfigManager configManager)[] providers)
        {
            var providersField = typeof(ExternalIdentityValidator)
                                     .GetField("_providers", BindingFlags.Instance | BindingFlags.NonPublic)
                                 ?? throw new InvalidOperationException("_providers field not found");

            // Get the ProviderConfiguration type from the same assembly (it's internal at namespace level)
            var assembly = typeof(ExternalIdentityValidator).Assembly;
            var providerConfigType = assembly.GetType("GovUK.Dfe.CoreLibs.Security.OpenIdConnect.ProviderConfiguration")
                                     ?? throw new InvalidOperationException("ProviderConfiguration type not found");

            // Create a new list and populate it
            var listType = typeof(List<>).MakeGenericType(providerConfigType);
            var list = Activator.CreateInstance(listType)!;
            var addMethod = listType.GetMethod("Add")!;

            foreach (var (opts, configManager) in providers)
            {
                var providerConfig = Activator.CreateInstance(
                    providerConfigType,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                    null,
                    new object[] { opts, configManager },
                    null);
                addMethod.Invoke(list, new[] { providerConfig });
            }

            providersField.SetValue(validator, list);
        }

        /// <summary>
        /// Simplified helper for single-provider tests (backward compatible scenarios).
        /// </summary>
        private void InjectSingleProvider(
            ExternalIdentityValidator validator,
            StubConfigManager configManager)
        {
            InjectProviders(validator, (_oidcOpts, configManager));
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
                    new Claim(ClaimTypes.Email, "user1@example.com")
                }),
                Expires = DateTime.UtcNow.AddMinutes(5),
                SigningCredentials = creds
            };
            var tokenString = handler.WriteToken(handler.CreateToken(descriptor));

            var openIdConfig = new OpenIdConnectConfiguration { Issuer = _oidcOpts.Issuer };
            openIdConfig.SigningKeys.Add(signingKey);

            var stubConfigManager = new StubConfigManager(openIdConfig);

            var httpClientFactory = CreateHttpClientFactory();

            var validator = new ExternalIdentityValidator(
                Options.Create(_oidcOpts),
                httpClientFactory);

            InjectSingleProvider(validator, stubConfigManager);

            // Act
            var principal = await validator.ValidateIdTokenAsync(tokenString);

            // Assert
            Assert.NotNull(principal);
            Assert.True(principal.Identity?.IsAuthenticated);
            Assert.Equal("user1", principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            validator.Dispose();
        }

        [Fact]
        public async Task ValidateIdTokenAsync_NullOrWhitespace_ThrowsArgumentNullException()
        {
            var stubManager = new StubConfigManager(new OpenIdConnectConfiguration());

            var httpClientFactory = CreateHttpClientFactory();

            var validator = new ExternalIdentityValidator(
                Options.Create(_oidcOpts),
                httpClientFactory);

            InjectSingleProvider(validator, stubManager);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => validator.ValidateIdTokenAsync(null!, false, false, CancellationToken.None));
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => validator.ValidateIdTokenAsync("", false, false, CancellationToken.None));
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => validator.ValidateIdTokenAsync("   ", false, false, CancellationToken.None));
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes_NoException()
        {
            var stubManager = new StubConfigManager(new OpenIdConnectConfiguration());
            var httpClientFactory = CreateHttpClientFactory();

            var validator = new ExternalIdentityValidator(
                Options.Create(_oidcOpts),
                httpClientFactory);

            InjectSingleProvider(validator, stubManager);

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
                    null!,
                    factory));
        }

        [Fact]
        public void Constructor_NoDiscoveryEndpoints_ThrowsArgumentException()
        {
            var factory = CreateHttpClientFactory();
            var opts = new OpenIdConnectOptions
            {
                Issuer = "https://idp.example.com/",
                DiscoveryEndpoint = null,
                DiscoveryEndpoints = null
            };

            Assert.Throws<ArgumentException>(() =>
                new ExternalIdentityValidator(
                    Options.Create(opts),
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
                JwtSigningKey = "any-key-that-is-long-enough-32ch",
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false
            };

            var validator = new ExternalIdentityValidator(
                Options.Create(_oidcOpts),
                factory,
                multiProviderOptions: null,
                cypressAuthOpts: null,
                testOptions: Options.Create(testOpts));

            Assert.True(validator.IsTestAuthenticationEnabled);
        }

        [Fact]
        public void ValidateTestIdToken_NullOrWhitespace_ThrowsArgumentNullException()
        {
            var factory = CreateHttpClientFactory();
            var testOpts = new TestAuthenticationOptions
            {
                Enabled = true,
                JwtSigningKey = "key-that-is-long-enough-32chars",
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false
            };
            var validator = new ExternalIdentityValidator(
                Options.Create(_oidcOpts),
                factory,
                multiProviderOptions: null,
                cypressAuthOpts: null,
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
                JwtSigningKey = "key-that-is-long-enough-32chars",
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false
            };
            var validator = new ExternalIdentityValidator(
                Options.Create(_oidcOpts),
                factory,
                multiProviderOptions: null,
                cypressAuthOpts: null,
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
                JwtSigningKey = "",
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false
            };
            var validator = new ExternalIdentityValidator(
                Options.Create(_oidcOpts),
                factory,
                multiProviderOptions: null,
                cypressAuthOpts: null,
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
                multiProviderOptions: null,
                cypressAuthOpts: null,
                testOptions: Options.Create(testOpts));

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
                multiProviderOptions: null,
                cypressAuthOpts: null,
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
        public async Task ValidateIdTokenAsync_WithTestOptionsEnabled_UsesTestPath()
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
                multiProviderOptions: null,
                cypressAuthOpts: null,
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
                multiProviderOptions: null,
                cypressAuthOpts: null,
                testOptions: null,
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
                SecretKey = "",
                Issuer = "internal-issuer",
                Audience = "internal-audience"
            };
            var validator = new ExternalIdentityValidator(
                Options.Create(_oidcOpts),
                factory,
                multiProviderOptions: null,
                cypressAuthOpts: null,
                testOptions: null,
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
                multiProviderOptions: null,
                cypressAuthOpts: null,
                testOptions: null,
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
                multiProviderOptions: null,
                cypressAuthOpts: null,
                testOptions: null,
                internalAuthOpts: Options.Create(internalAuthOpts));

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
                multiProviderOptions: null,
                cypressAuthOpts: null,
                testOptions: null,
                internalAuthOpts: Options.Create(internalAuthOpts));

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(rawKey));
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            var handler = new JwtSecurityTokenHandler();
            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = "wrong-issuer",
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
                multiProviderOptions: null,
                cypressAuthOpts: null,
                testOptions: null,
                internalAuthOpts: Options.Create(internalAuthOpts));

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(rawKey));
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            var handler = new JwtSecurityTokenHandler();
            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = "internal-issuer",
                Audience = "wrong-audience",
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
                multiProviderOptions: null,
                cypressAuthOpts: null,
                testOptions: null,
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
                multiProviderOptions: null,
                cypressAuthOpts: null,
                testOptions: null,
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
                multiProviderOptions: null,
                cypressAuthOpts: null,
                testOptions: null,
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

            var validator = new ExternalIdentityValidator(
                Options.Create(_oidcOpts),
                factory,
                multiProviderOptions: null,
                cypressAuthOpts: null,
                testOptions: null,
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
                    new Claim(ClaimTypes.NameIdentifier, "user")
                }),
                Expires = DateTime.UtcNow.AddMinutes(5),
                SigningCredentials = creds
            };
            var token = handler.WriteToken(handler.CreateToken(descriptor));

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
                multiProviderOptions: null,
                cypressAuthOpts: null,
                testOptions: null,
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
                NotBefore = DateTime.UtcNow.AddMinutes(-15),
                Expires = DateTime.UtcNow.AddMinutes(-10),
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
                multiProviderOptions: null,
                cypressAuthOpts: null,
                testOptions: null,
                internalAuthOpts: Options.Create(internalAuthOpts));

            var malformedToken = "this.is.not.a.valid.jwt.token";

            var exception = Assert.Throws<SecurityTokenException>(() =>
                validator.ValidateInternalAuthToken(malformedToken));
            Assert.Contains("Internal Auth token validation failed", exception.Message);
        }

        #endregion

        #region Multi-Provider Tests

        [Fact]
        public async Task ValidateIdTokenAsync_MultipleDiscoveryEndpoints_CollectsKeysFromAll()
        {
            // Keys must be at least 32 bytes for HS256
            var key1Bytes = Encoding.UTF8.GetBytes("PROVIDER1KEY12345678901234567890AB");
            var key2Bytes = Encoding.UTF8.GetBytes("PROVIDER2KEY12345678901234567890CD");
            var signingKey1 = new SymmetricSecurityKey(key1Bytes);
            var signingKey2 = new SymmetricSecurityKey(key2Bytes);

            var config1 = new OpenIdConnectConfiguration { Issuer = "https://provider1.example.com/" };
            config1.SigningKeys.Add(signingKey1);

            var config2 = new OpenIdConnectConfiguration { Issuer = "https://provider2.example.com/" };
            config2.SigningKeys.Add(signingKey2);

            var stubManager1 = new StubConfigManager(config1);
            var stubManager2 = new StubConfigManager(config2);

            var multiOpts = new OpenIdConnectOptions
            {
                DiscoveryEndpoint = "https://provider1.example.com/.well-known/openid-configuration",
                ValidIssuers = new List<string>
                {
                    "https://provider1.example.com/",
                    "https://provider2.example.com/"
                },
                ValidateIssuer = true,
                ValidateAudience = false,
                ValidateLifetime = true
            };

            var opts1 = new OpenIdConnectOptions
            {
                Issuer = "https://provider1.example.com/",
                DiscoveryEndpoint = "https://provider1.example.com/.well-known",
                ValidateIssuer = true,
                ValidateAudience = false,
                ValidateLifetime = true
            };

            var opts2 = new OpenIdConnectOptions
            {
                Issuer = "https://provider2.example.com/",
                DiscoveryEndpoint = "https://provider2.example.com/.well-known",
                ValidateIssuer = true,
                ValidateAudience = false,
                ValidateLifetime = true
            };

            var httpClientFactory = CreateHttpClientFactory();
            var validator = new ExternalIdentityValidator(
                Options.Create(multiOpts),
                httpClientFactory);

            // Inject BOTH providers
            InjectProviders(validator,
                (opts1, stubManager1),
                (opts2, stubManager2));

            // Create a token signed by provider2's key
            var creds = new SigningCredentials(signingKey2, SecurityAlgorithms.HmacSha256);
            var handler = new JwtSecurityTokenHandler();
            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = "https://provider2.example.com/",
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "multi-provider-user"),
                    new Claim(ClaimTypes.Email, "user@provider2.com")
                }),
                Expires = DateTime.UtcNow.AddMinutes(5),
                SigningCredentials = creds
            };
            var token = handler.WriteToken(handler.CreateToken(descriptor));

            var principal = await validator.ValidateIdTokenAsync(token);

            Assert.NotNull(principal);
            Assert.True(principal.Identity?.IsAuthenticated);
            Assert.Equal("multi-provider-user", principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            validator.Dispose();
        }

        [Fact]
        public async Task ValidateIdTokenAsync_WithValidAudiences_ValidatesAgainstMultiple()
        {
            var secretKey = Encoding.UTF8.GetBytes("0123456789ABCDEF0123456789ABCDEF");
            var signingKey = new SymmetricSecurityKey(secretKey);
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var handler = new JwtSecurityTokenHandler();
            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = "https://idp.example.com/",
                Audience = "client-b",
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "audience-test-user")
                }),
                Expires = DateTime.UtcNow.AddMinutes(5),
                SigningCredentials = creds
            };
            var token = handler.WriteToken(handler.CreateToken(descriptor));

            var openIdConfig = new OpenIdConnectConfiguration { Issuer = "https://idp.example.com/" };
            openIdConfig.SigningKeys.Add(signingKey);
            var stubManager = new StubConfigManager(openIdConfig);

            var opts = new OpenIdConnectOptions
            {
                Issuer = "https://idp.example.com/",
                DiscoveryEndpoint = "https://idp.example.com/.well-known/openid-configuration",
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidAudiences = new List<string> { "client-a", "client-b", "client-c" },
                ValidateLifetime = true
            };

            var httpClientFactory = CreateHttpClientFactory();
            var validator = new ExternalIdentityValidator(
                Options.Create(opts),
                httpClientFactory);

            InjectProviders(validator, (opts, stubManager));

            var principal = await validator.ValidateIdTokenAsync(token);

            Assert.NotNull(principal);
            Assert.True(principal.Identity?.IsAuthenticated);
            Assert.Equal("audience-test-user", principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            validator.Dispose();
        }

        [Fact]
        public void Constructor_WithDiscoveryEndpointsArray_CreatesMultipleProviders()
        {
            var opts = new OpenIdConnectOptions
            {
                DiscoveryEndpoints = new List<string>
                {
                    "https://provider1.example.com/.well-known/openid-configuration",
                    "https://provider2.example.com/.well-known/openid-configuration"
                },
                ValidIssuers = new List<string>
                {
                    "https://provider1.example.com/",
                    "https://provider2.example.com/"
                }
            };

            var httpClientFactory = CreateHttpClientFactory();

            var validator = new ExternalIdentityValidator(
                Options.Create(opts),
                httpClientFactory);

            Assert.Equal(2, validator.ProviderCount);

            validator.Dispose();
        }

        [Fact]
        public void Constructor_WithMultiProviderOptions_SetsMultiProviderMode()
        {
            var httpClientFactory = CreateHttpClientFactory();

            var multiProviderOpts = new MultiProviderOpenIdConnectOptions
            {
                Providers = new List<OpenIdConnectOptions>
                {
                    new OpenIdConnectOptions
                    {
                        Issuer = "https://tenant1.example.com/",
                        ClientId = "client-tenant1",
                        DiscoveryEndpoint = "https://tenant1.example.com/.well-known/openid-configuration"
                    },
                    new OpenIdConnectOptions
                    {
                        Issuer = "https://tenant2.example.com/",
                        ClientId = "client-tenant2",
                        DiscoveryEndpoint = "https://tenant2.example.com/.well-known/openid-configuration"
                    }
                }
            };

            var validator = new ExternalIdentityValidator(
                Options.Create(_oidcOpts),
                httpClientFactory,
                multiProviderOptions: Options.Create(multiProviderOpts));

            Assert.True(validator.IsMultiProviderMode);
            Assert.Equal(2, validator.ProviderCount);

            validator.Dispose();
        }

        [Fact]
        public async Task ValidateIdTokenAsync_MultiProviderMode_ValidatesAgainstCorrectProvider()
        {
            var httpClientFactory = CreateHttpClientFactory();

            // Two different providers with different keys
            var key1 = Encoding.UTF8.GetBytes("TENANT1KEY123456789012345678901234");
            var key2 = Encoding.UTF8.GetBytes("TENANT2KEY123456789012345678901234");
            var signingKey1 = new SymmetricSecurityKey(key1);
            var signingKey2 = new SymmetricSecurityKey(key2);

            var config1 = new OpenIdConnectConfiguration { Issuer = "https://tenant1.example.com/" };
            config1.SigningKeys.Add(signingKey1);

            var config2 = new OpenIdConnectConfiguration { Issuer = "https://tenant2.example.com/" };
            config2.SigningKeys.Add(signingKey2);

            var opts1 = new OpenIdConnectOptions
            {
                Issuer = "https://tenant1.example.com/",
                ClientId = "client-tenant1",
                DiscoveryEndpoint = "https://tenant1.example.com/.well-known",
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true
            };

            var opts2 = new OpenIdConnectOptions
            {
                Issuer = "https://tenant2.example.com/",
                ClientId = "client-tenant2",
                DiscoveryEndpoint = "https://tenant2.example.com/.well-known",
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true
            };

            var multiProviderOpts = new MultiProviderOpenIdConnectOptions
            {
                Providers = new List<OpenIdConnectOptions> { opts1, opts2 }
            };

            var validator = new ExternalIdentityValidator(
                Options.Create(_oidcOpts),
                httpClientFactory,
                multiProviderOptions: Options.Create(multiProviderOpts));

            // Inject stub managers
            InjectProviders(validator,
                (opts1, new StubConfigManager(config1)),
                (opts2, new StubConfigManager(config2)));

            // Token from tenant2
            var creds = new SigningCredentials(signingKey2, SecurityAlgorithms.HmacSha256);
            var handler = new JwtSecurityTokenHandler();
            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = "https://tenant2.example.com/",
                Audience = "client-tenant2",
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "tenant2-user")
                }),
                Expires = DateTime.UtcNow.AddMinutes(5),
                SigningCredentials = creds
            };
            var token = handler.WriteToken(handler.CreateToken(descriptor));

            var principal = await validator.ValidateIdTokenAsync(token);

            Assert.NotNull(principal);
            Assert.Equal("tenant2-user", principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            validator.Dispose();
        }

        [Fact]
        public async Task ValidateIdTokenAsync_MultiProviderMode_CrossTenantToken_Fails()
        {
            var httpClientFactory = CreateHttpClientFactory();

            var key1 = Encoding.UTF8.GetBytes("TENANT1KEY123456789012345678901234");
            var key2 = Encoding.UTF8.GetBytes("TENANT2KEY123456789012345678901234");
            var signingKey1 = new SymmetricSecurityKey(key1);
            var signingKey2 = new SymmetricSecurityKey(key2);

            var config1 = new OpenIdConnectConfiguration { Issuer = "https://tenant1.example.com/" };
            config1.SigningKeys.Add(signingKey1);

            var config2 = new OpenIdConnectConfiguration { Issuer = "https://tenant2.example.com/" };
            config2.SigningKeys.Add(signingKey2);

            var opts1 = new OpenIdConnectOptions
            {
                Issuer = "https://tenant1.example.com/",
                ClientId = "client-tenant1",
                DiscoveryEndpoint = "https://tenant1.example.com/.well-known",
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true
            };

            var opts2 = new OpenIdConnectOptions
            {
                Issuer = "https://tenant2.example.com/",
                ClientId = "client-tenant2",
                DiscoveryEndpoint = "https://tenant2.example.com/.well-known",
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true
            };

            var multiProviderOpts = new MultiProviderOpenIdConnectOptions
            {
                Providers = new List<OpenIdConnectOptions> { opts1, opts2 }
            };

            var validator = new ExternalIdentityValidator(
                Options.Create(_oidcOpts),
                httpClientFactory,
                multiProviderOptions: Options.Create(multiProviderOpts));

            InjectProviders(validator,
                (opts1, new StubConfigManager(config1)),
                (opts2, new StubConfigManager(config2)));

            // Token signed by tenant1's key but with tenant2's issuer (cross-tenant attack)
            var creds = new SigningCredentials(signingKey1, SecurityAlgorithms.HmacSha256);
            var handler = new JwtSecurityTokenHandler();
            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = "https://tenant2.example.com/",  // Wrong issuer for this key
                Audience = "client-tenant2",
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "attacker")
                }),
                Expires = DateTime.UtcNow.AddMinutes(5),
                SigningCredentials = creds
            };
            var token = handler.WriteToken(handler.CreateToken(descriptor));

            // Should fail because the key doesn't match tenant2's expected key
            await Assert.ThrowsAsync<SecurityTokenValidationException>(() =>
                validator.ValidateIdTokenAsync(token));

            validator.Dispose();
        }

        [Fact]
        public void OpenIdConnectOptions_GetAllDiscoveryEndpoints_ReturnsArrayWhenPopulated()
        {
            var opts = new OpenIdConnectOptions
            {
                DiscoveryEndpoint = "https://single.example.com/.well-known",
                DiscoveryEndpoints = new List<string>
                {
                    "https://multi1.example.com/.well-known",
                    "https://multi2.example.com/.well-known"
                }
            };

            var endpoints = opts.GetAllDiscoveryEndpoints().ToList();
            Assert.Equal(2, endpoints.Count);
            Assert.Contains("https://multi1.example.com/.well-known", endpoints);
            Assert.Contains("https://multi2.example.com/.well-known", endpoints);
        }

        [Fact]
        public void OpenIdConnectOptions_GetAllDiscoveryEndpoints_FallsBackToSingle()
        {
            var opts = new OpenIdConnectOptions
            {
                DiscoveryEndpoint = "https://single.example.com/.well-known",
                DiscoveryEndpoints = null
            };

            var endpoints = opts.GetAllDiscoveryEndpoints().ToList();
            Assert.Single(endpoints);
            Assert.Equal("https://single.example.com/.well-known", endpoints[0]);
        }

        [Fact]
        public void OpenIdConnectOptions_GetAllValidIssuers_ReturnsArrayWhenPopulated()
        {
            var opts = new OpenIdConnectOptions
            {
                Issuer = "https://single-issuer.example.com/",
                ValidIssuers = new List<string>
                {
                    "https://issuer1.example.com/",
                    "https://issuer2.example.com/"
                }
            };

            var issuers = opts.GetAllValidIssuers().ToList();
            Assert.Equal(2, issuers.Count);
            Assert.Contains("https://issuer1.example.com/", issuers);
            Assert.Contains("https://issuer2.example.com/", issuers);
        }

        [Fact]
        public void OpenIdConnectOptions_GetAllValidAudiences_ReturnsArrayWhenPopulated()
        {
            var opts = new OpenIdConnectOptions
            {
                ClientId = "single-client",
                ValidAudiences = new List<string> { "client-a", "client-b" }
            };

            var audiences = opts.GetAllValidAudiences().ToList();
            Assert.Equal(2, audiences.Count);
            Assert.Contains("client-a", audiences);
            Assert.Contains("client-b", audiences);
        }

        #endregion
    }
}
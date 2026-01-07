using GovUK.Dfe.CoreLibs.Security.Configurations;
using GovUK.Dfe.CoreLibs.Security.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GovUK.Dfe.CoreLibs.Security.OpenIdConnect
{
    /// <summary>
    /// An implementation of <see cref="IExternalIdentityValidator"/> that uses the
    /// Microsoft.IdentityModel.Protocols stack to retrieve metadata and signing keys
    /// from one or more OpenID Connect providers, caching them automatically.
    /// Supports multi-tenant scenarios with multiple OIDC providers.
    /// Also supports test token validation for development/testing scenarios.
    /// </summary>
    public sealed class ExternalIdentityValidator
        : IExternalIdentityValidator, IDisposable
    {
        private readonly List<ConfigurationManager<OpenIdConnectConfiguration>> _configManagers;
        private readonly OpenIdConnectOptions _opts;
        private readonly TestAuthenticationOptions? _testOpts;
        private readonly CypressAuthenticationOptions? _cypressAuthOpts;
        private readonly InternalServiceAuthOptions? _internalAuthOpts;

        /// <summary>
        /// Initializes a new instance of <see cref="ExternalIdentityValidator"/>.
        /// </summary>
        /// <param name="options">
        /// The OIDC validation options, bound from configuration.
        /// Supports single provider (Issuer + DiscoveryEndpoint) or multiple providers
        /// (ValidIssuers + DiscoveryEndpoints).
        /// </param>
        /// <param name="httpClientFactory">
        /// Factory for creating <see cref="System.Net.Http.HttpClient"/> instances
        /// to fetch the discovery document and JWKS.
        /// </param>
        /// <param name="cypressAuthOpts">Cypress authentication options</param>
        /// <param name="testOptions">
        /// Optional test authentication options for development/testing scenarios.
        /// </param>
        /// <param name="internalAuthOpts">
        /// Internal Authentication scheme options
        /// </param>
        public ExternalIdentityValidator(
            IOptions<OpenIdConnectOptions> options,
            IHttpClientFactory httpClientFactory,
            IOptions<CypressAuthenticationOptions>? cypressAuthOpts = null,
            IOptions<TestAuthenticationOptions>? testOptions = null,
            IOptions<InternalServiceAuthOptions>? internalAuthOpts = null)
        {
            _opts = options?.Value
                    ?? throw new ArgumentNullException(nameof(options));

            _testOpts = testOptions?.Value;
            _cypressAuthOpts = cypressAuthOpts?.Value;
            _internalAuthOpts = internalAuthOpts?.Value;

            // Create ConfigurationManager instances for all discovery endpoints
            _configManagers = new List<ConfigurationManager<OpenIdConnectConfiguration>>();

            var discoveryEndpoints = _opts.GetAllDiscoveryEndpoints().ToList();

            if (!discoveryEndpoints.Any())
            {
                throw new ArgumentException(
                    "At least one discovery endpoint must be configured (DiscoveryEndpoint or DiscoveryEndpoints).",
                    nameof(options));
            }

            foreach (var endpoint in discoveryEndpoints)
            {
                var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                    metadataAddress: endpoint,
                    configRetriever: new OpenIdConnectConfigurationRetriever(),
                    docRetriever: new HttpDocumentRetriever(
                        httpClientFactory.CreateClient())
                    {
                        RequireHttps = endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                    });

                _configManagers.Add(configManager);
            }
        }

        /// <inheritdoc/>
        public async Task<ClaimsPrincipal> ValidateIdTokenAsync(
            string idToken,
            bool validCypressRequest = false,
            bool validInternalRequest = false,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(idToken))
                throw new ArgumentNullException(nameof(idToken));

            // Check if internal authentication is enabled and should be used
            if (!string.IsNullOrEmpty(_internalAuthOpts?.SecretKey) && validInternalRequest)
            {
                return ValidateInternalAuthToken(idToken);
            }

            // Check if test authentication is enabled and should be used
            if (_testOpts?.Enabled == true || validCypressRequest)
            {
                return ValidateTestIdToken(idToken, validCypressRequest);
            }

            // Collect signing keys from ALL configured OIDC providers
            var allSigningKeys = new List<SecurityKey>();
            foreach (var configManager in _configManagers)
            {
                var metadata = await configManager.GetConfigurationAsync(cancellationToken);
                allSigningKeys.AddRange(metadata.SigningKeys);
            }

            // Get all valid issuers and audiences
            var validIssuers = _opts.GetAllValidIssuers().ToList();
            var validAudiences = _opts.GetAllValidAudiences().ToList();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = _opts.ValidateIssuer,
                ValidIssuers = validIssuers.Any() ? validIssuers : null,
                ValidateAudience = _opts.ValidateAudience,
                ValidAudiences = validAudiences.Any() ? validAudiences : null,
                ValidateLifetime = _opts.ValidateLifetime,
                IssuerSigningKeys = allSigningKeys,
                ValidateIssuerSigningKey = true
            };

            var handler = new JwtSecurityTokenHandler();
            return handler.ValidateToken(
                idToken,
                validationParameters,
                out _ /* validatedToken */);
        }

        /// <summary>
        /// Validates a test ID token using the configured test authentication options.
        /// This method bypasses OIDC discovery and uses a pre-configured signing key.
        /// </summary>
        /// <param name="idToken">The test JWT token to validate</param>
        /// <param name="cypressRequest">Whether this is a cypress request</param>
        /// <returns>A ClaimsPrincipal containing the validated claims</returns>
        /// <exception cref="ArgumentNullException">Thrown when idToken is null or empty</exception>
        /// <exception cref="InvalidOperationException">Thrown when test authentication is not properly configured</exception>
        /// <exception cref="SecurityTokenException">Thrown when token validation fails</exception>
        public ClaimsPrincipal ValidateTestIdToken(string idToken, bool cypressRequest = false)
        {
            if (string.IsNullOrWhiteSpace(idToken))
                throw new ArgumentNullException(nameof(idToken));

            if (_testOpts == null || (!_testOpts.Enabled && !cypressRequest))
                throw new InvalidOperationException("Test authentication is not enabled or configured.");

            if (string.IsNullOrWhiteSpace(_testOpts.JwtSigningKey))
                throw new InvalidOperationException("Test JWT signing key is not configured.");

            // Create signing key from the configured test key
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_testOpts.JwtSigningKey));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = _testOpts.ValidateIssuer,
                ValidIssuer = _testOpts.ValidateIssuer ? _testOpts.JwtIssuer : null,
                ValidateAudience = _testOpts.ValidateAudience,
                ValidAudience = _testOpts.ValidateAudience ? _testOpts.JwtAudience : null,
                ValidateLifetime = _testOpts.ValidateLifetime,
                IssuerSigningKey = key,
                ValidateIssuerSigningKey = true,
                // Allow some clock skew for test scenarios
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            var handler = new JwtSecurityTokenHandler();

            try
            {
                return handler.ValidateToken(
                    idToken,
                    validationParameters,
                    out _ /* validatedToken */);
            }
            catch (Exception ex)
            {
                throw new SecurityTokenException($"Test token validation failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validates an Internal Auth Id token using the configured authentication options.
        /// This method bypasses OIDC discovery and uses a pre-configured signing key.
        /// </summary>
        /// <param name="idToken">The JWT token to validate</param>
        /// <returns>A ClaimsPrincipal containing the validated claims</returns>
        /// <exception cref="ArgumentNullException">Thrown when idToken is null or empty</exception>
        /// <exception cref="InvalidOperationException">Thrown when test authentication is not properly configured</exception>
        /// <exception cref="SecurityTokenException">Thrown when token validation fails</exception>
        public ClaimsPrincipal ValidateInternalAuthToken(string idToken)
        {
            if (string.IsNullOrWhiteSpace(idToken))
                throw new ArgumentNullException(nameof(idToken));

            if (string.IsNullOrWhiteSpace(_internalAuthOpts?.SecretKey))
                throw new InvalidOperationException("Test JWT signing key is not configured.");

            // Create signing key from the configured test key
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_internalAuthOpts?.SecretKey!));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _internalAuthOpts?.Issuer,
                ValidateAudience = true,
                ValidAudience = _internalAuthOpts?.Audience,
                IssuerSigningKey = key,
                ValidateIssuerSigningKey = true,
            };

            var handler = new JwtSecurityTokenHandler();

            try
            {
                return handler.ValidateToken(
                    idToken,
                    validationParameters,
                    out _ /* validatedToken */);
            }
            catch (Exception ex)
            {
                throw new SecurityTokenException($"Internal Auth token validation failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Determines whether test authentication is enabled and configured.
        /// </summary>
        /// <returns>True if test authentication is enabled, false otherwise</returns>
        public bool IsTestAuthenticationEnabled => _testOpts?.Enabled == true;

        /// <summary>
        /// Disposes all internal <see cref="ConfigurationManager{OpenIdConnectConfiguration}"/> instances,
        /// which stops their background metadata refresh timers.
        /// </summary>
        public void Dispose()
        {
            foreach (var configManager in _configManagers)
            {
                if (configManager is IDisposable disposable)
                    disposable.Dispose();
            }
            _configManagers.Clear();
        }
    }
}
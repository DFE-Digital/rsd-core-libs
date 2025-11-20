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
    /// from an OpenID Connect provider, caching them automatically.
    /// Also supports test token validation for development/testing scenarios.
    /// </summary>
    public sealed class ExternalIdentityValidator
        : IExternalIdentityValidator, IDisposable
    {
        private readonly ConfigurationManager<OpenIdConnectConfiguration> _configManager;
        private readonly OpenIdConnectOptions _opts;
        private readonly TestAuthenticationOptions? _testOpts;
        private readonly CypressAuthenticationOptions? _cypressAuthOpts;

        /// <summary>
        /// Initializes a new instance of <see cref="ExternalIdentityValidator"/>.
        /// </summary>
        /// <param name="options">
        /// The OIDC validation options, bound from configuration (issuer + discovery endpoint).
        /// </param>
        /// <param name="httpClientFactory">
        /// Factory for creating <see cref="System.Net.Http.HttpClient"/> instances
        /// to fetch the discovery document and JWKS.
        /// </param>
        /// <param name="cypressAuthOpts">Cypress authentication options</param>
        /// <param name="testOptions">
        /// Optional test authentication options for development/testing scenarios.
        /// </param>
        public ExternalIdentityValidator(
            IOptions<OpenIdConnectOptions> options,
            IHttpClientFactory httpClientFactory,
            IOptions<CypressAuthenticationOptions>? cypressAuthOpts = null, 
            IOptions<TestAuthenticationOptions>? testOptions = null)
        {
            _opts = options?.Value
                    ?? throw new ArgumentNullException(nameof(options));

            _testOpts = testOptions?.Value;

            _cypressAuthOpts = cypressAuthOpts?.Value;

            // Use the built-in ConfigurationManager to handle metadata caching/refresh.
            _configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                metadataAddress: _opts.DiscoveryEndpoint,
                configRetriever: new OpenIdConnectConfigurationRetriever(),
                docRetriever: new HttpDocumentRetriever(
                    httpClientFactory.CreateClient())
                {
                    RequireHttps = _opts.DiscoveryEndpoint!
                        .StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                });
        }

        /// <inheritdoc/>
        public async Task<ClaimsPrincipal> ValidateIdTokenAsync(
            string idToken,
            bool validCypressRequest = false,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(idToken))
                throw new ArgumentNullException(nameof(idToken));

            // Check if test authentication is enabled and should be used
            if (_testOpts?.Enabled == true || validCypressRequest)
            {
                return ValidateTestIdToken(idToken, validCypressRequest);
            }

            // Fetch (or retrieve cached) OIDC metadata & signing keys
            var metadata =
                await _configManager.GetConfigurationAsync(cancellationToken);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = _opts.ValidateIssuer,
                ValidIssuer = _opts.Issuer,
                ValidateAudience = _opts.ValidateAudience,
                ValidateLifetime = _opts.ValidateLifetime,
                IssuerSigningKeys = metadata.SigningKeys
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
        /// Determines whether test authentication is enabled and configured.
        /// </summary>
        /// <returns>True if test authentication is enabled, false otherwise</returns>
        public bool IsTestAuthenticationEnabled => _testOpts?.Enabled == true;
 
        /// <summary>
        /// Disposes the internal <see cref="ConfigurationManager{OpenIdConnectConfiguration}"/>,
        /// which stops its background metadata refresh timer.
        /// </summary>
        public void Dispose()
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            if (_configManager is IDisposable disposable)
                disposable.Dispose();
        }
    }
}

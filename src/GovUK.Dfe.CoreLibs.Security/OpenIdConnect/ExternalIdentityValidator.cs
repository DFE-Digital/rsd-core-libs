using GovUK.Dfe.CoreLibs.Security.Configurations;
using GovUK.Dfe.CoreLibs.Security.Interfaces;
using Microsoft.Extensions.Logging;
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
    /// Represents a configured OIDC provider with its options and configuration manager.
    /// </summary>
    internal sealed class ProviderConfiguration : IDisposable
    {
        public OpenIdConnectOptions Options { get; }
        public ConfigurationManager<OpenIdConnectConfiguration> ConfigManager { get; }

        public ProviderConfiguration(
            OpenIdConnectOptions options,
            ConfigurationManager<OpenIdConnectConfiguration> configManager)
        {
            Options = options;
            ConfigManager = configManager;
        }

        public void Dispose()
        {
            if (ConfigManager is IDisposable disposable)
                disposable.Dispose();
        }
    }

    /// <summary>
    /// An implementation of <see cref="IExternalIdentityValidator"/> that uses the
    /// Microsoft.IdentityModel.Protocols stack to retrieve metadata and signing keys
    /// from one or more OpenID Connect providers, caching them automatically.
    /// Supports multi-tenant scenarios with multiple isolated OIDC providers.
    /// Also supports test token validation for development/testing scenarios.
    /// </summary>
    public sealed class ExternalIdentityValidator
        : IExternalIdentityValidator, IDisposable
    {
        private readonly List<ProviderConfiguration> _providers;
        private readonly OpenIdConnectOptions? _singleProviderOpts;
        private readonly TestAuthenticationOptions? _testOpts;
        private readonly CypressAuthenticationOptions? _cypressAuthOpts;
        private readonly InternalServiceAuthOptions? _internalAuthOpts;
        private readonly ILogger<ExternalIdentityValidator>? _logger;
        private readonly bool _isMultiProviderMode;

        /// <summary>
        /// Initializes a new instance of <see cref="ExternalIdentityValidator"/>.
        /// Supports both single-provider (backward compatible) and multi-provider modes.
        /// </summary>
        public ExternalIdentityValidator(
            IOptions<OpenIdConnectOptions> options,
            IHttpClientFactory httpClientFactory,
            IOptions<MultiProviderOpenIdConnectOptions>? multiProviderOptions = null,
            IOptions<CypressAuthenticationOptions>? cypressAuthOpts = null,
            IOptions<TestAuthenticationOptions>? testOptions = null,
            IOptions<InternalServiceAuthOptions>? internalAuthOpts = null,
            ILogger<ExternalIdentityValidator>? logger = null)
        {
            _testOpts = testOptions?.Value;
            _cypressAuthOpts = cypressAuthOpts?.Value;
            _internalAuthOpts = internalAuthOpts?.Value;
            _logger = logger;
            _providers = new List<ProviderConfiguration>();

            var multiOpts = multiProviderOptions?.Value;

            // Check if multi-provider mode should be used
            if (multiOpts?.Providers != null && multiOpts.Providers.Any())
            {
                _isMultiProviderMode = true;
                _singleProviderOpts = null;

                foreach (var providerOpts in multiOpts.Providers)
                {
                    var endpoint = providerOpts.DiscoveryEndpoint;
                    if (string.IsNullOrEmpty(endpoint))
                    {
                        throw new ArgumentException(
                            $"Each provider must have a DiscoveryEndpoint configured. " +
                            $"Provider with Issuer '{providerOpts.Issuer}' is missing DiscoveryEndpoint.");
                    }

                    var configManager = CreateConfigManager(endpoint, httpClientFactory);
                    _providers.Add(new ProviderConfiguration(providerOpts, configManager));
                }

                _logger?.LogInformation(
                    "ExternalIdentityValidator initialized in multi-provider mode with {Count} providers",
                    _providers.Count);
            }
            else
            {
                // Single-provider mode (backward compatible)
                _isMultiProviderMode = false;
                _singleProviderOpts = options?.Value
                    ?? throw new ArgumentNullException(nameof(options));

                var discoveryEndpoints = _singleProviderOpts.GetAllDiscoveryEndpoints().ToList();

                if (!discoveryEndpoints.Any())
                {
                    throw new ArgumentException(
                        "At least one discovery endpoint must be configured (DiscoveryEndpoint or DiscoveryEndpoints).",
                        nameof(options));
                }

                foreach (var endpoint in discoveryEndpoints)
                {
                    var configManager = CreateConfigManager(endpoint, httpClientFactory);
                    _providers.Add(new ProviderConfiguration(_singleProviderOpts, configManager));
                }

                _logger?.LogInformation(
                    "ExternalIdentityValidator initialized in single-provider mode with {Count} discovery endpoints",
                    discoveryEndpoints.Count);
            }
        }

        private static ConfigurationManager<OpenIdConnectConfiguration> CreateConfigManager(
            string endpoint,
            IHttpClientFactory httpClientFactory)
        {
            return new ConfigurationManager<OpenIdConnectConfiguration>(
                metadataAddress: endpoint,
                configRetriever: new OpenIdConnectConfigurationRetriever(),
                docRetriever: new HttpDocumentRetriever(httpClientFactory.CreateClient())
                {
                    RequireHttps = endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                });
        }

        /// <inheritdoc/>
        public Task<ClaimsPrincipal> ValidateIdTokenAsync(
            string idToken,
            bool validCypressRequest = false,
            bool validInternalRequest = false,
            CancellationToken cancellationToken = default)
        {
            // Use the configured internal auth options (backward compatible)
            return ValidateIdTokenAsync(
                idToken,
                validCypressRequest,
                validInternalRequest,
                internalAuthOptions: null,
                cancellationToken);
        }

        /// <inheritdoc/>
        public Task<ClaimsPrincipal> ValidateIdTokenAsync(
            string idToken,
            bool validCypressRequest,
            bool validInternalRequest,
            InternalServiceAuthOptions? internalAuthOptions,
            CancellationToken cancellationToken = default)
        {
            return ValidateIdTokenAsync(
                idToken, validCypressRequest, validInternalRequest,
                internalAuthOptions, testAuthOptions: null, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<ClaimsPrincipal> ValidateIdTokenAsync(
            string idToken,
            bool validCypressRequest,
            bool validInternalRequest,
            InternalServiceAuthOptions? internalAuthOptions,
            TestAuthenticationOptions? testAuthOptions,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(idToken))
                throw new ArgumentNullException(nameof(idToken));

            // Use provided options or fall back to configured defaults
            var effectiveInternalAuthOpts = internalAuthOptions ?? _internalAuthOpts;
            var effectiveTestOpts = testAuthOptions ?? _testOpts;

            // Check if internal authentication is enabled and should be used
            if (!string.IsNullOrEmpty(effectiveInternalAuthOpts?.SecretKey) && validInternalRequest)
            {
                return ValidateInternalAuthToken(idToken, effectiveInternalAuthOpts);
            }

            // Check if test authentication is enabled and should be used
            if (effectiveTestOpts?.Enabled == true || validCypressRequest)
            {
                return ValidateTestIdToken(idToken, validCypressRequest, effectiveTestOpts);
            }

            if (_isMultiProviderMode)
            {
                return await ValidateWithMultiProviderModeAsync(idToken, cancellationToken);
            }
            else
            {
                return await ValidateWithSingleProviderModeAsync(idToken, cancellationToken);
            }
        }

        /// <summary>
        /// Multi-provider mode: Try each provider until one fully validates.
        /// Token must match the provider's issuer AND audience.
        /// </summary>
        private async Task<ClaimsPrincipal> ValidateWithMultiProviderModeAsync(
            string idToken,
            CancellationToken cancellationToken)
        {
            var handler = new JwtSecurityTokenHandler();
            var errors = new List<string>();

            foreach (var provider in _providers)
            {
                try
                {
                    var metadata = await provider.ConfigManager.GetConfigurationAsync(cancellationToken);

                    // Build validation parameters for THIS specific provider
                    var validIssuers = GetValidIssuersForProvider(provider.Options);
                    var validAudiences = GetValidAudiencesForProvider(provider.Options);

                    var validationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = provider.Options.ValidateIssuer,
                        ValidIssuers = validIssuers.Any() ? validIssuers : null,
                        ValidIssuer = !validIssuers.Any() ? provider.Options.Issuer : null,
                        ValidateAudience = provider.Options.ValidateAudience,
                        ValidAudiences = validAudiences.Any() ? validAudiences : null,
                        ValidAudience = !validAudiences.Any() ? provider.Options.ClientId : null,
                        ValidateLifetime = provider.Options.ValidateLifetime,
                        IssuerSigningKeys = metadata.SigningKeys,
                        ValidateIssuerSigningKey = true
                    };

                    var principal = handler.ValidateToken(idToken, validationParameters, out _);

                    _logger?.LogDebug(
                        "Token validated successfully against provider with Issuer: {Issuer}",
                        provider.Options.Issuer);

                    return principal;
                }
                catch (SecurityTokenException ex)
                {
                    // This provider didn't match, try the next one
                    errors.Add($"Provider '{provider.Options.Issuer}': {ex.Message}");
                    _logger?.LogDebug(
                        "Token did not validate against provider {Issuer}: {Error}",
                        provider.Options.Issuer, ex.Message);
                    continue;
                }
            }

            // None of the providers could validate the token
            var errorMessage = $"Token did not match any configured OIDC provider. " +
                             $"Tried {_providers.Count} provider(s). Errors: {string.Join("; ", errors)}";
            _logger?.LogWarning(errorMessage);
            throw new SecurityTokenValidationException(errorMessage);
        }

        /// <summary>
        /// Single-provider mode (backward compatible): Collect all signing keys and validate
        /// against all configured issuers/audiences.
        /// </summary>
        private async Task<ClaimsPrincipal> ValidateWithSingleProviderModeAsync(
            string idToken,
            CancellationToken cancellationToken)
        {
            // Collect signing keys from ALL configured discovery endpoints
            var allSigningKeys = new List<SecurityKey>();
            foreach (var provider in _providers)
            {
                var metadata = await provider.ConfigManager.GetConfigurationAsync(cancellationToken);
                allSigningKeys.AddRange(metadata.SigningKeys);
            }

            // Get all valid issuers and audiences from the single options
            var validIssuers = _singleProviderOpts!.GetAllValidIssuers().ToList();
            var validAudiences = _singleProviderOpts.GetAllValidAudiences().ToList();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = _singleProviderOpts.ValidateIssuer,
                ValidIssuers = validIssuers.Any() ? validIssuers : null,
                ValidateAudience = _singleProviderOpts.ValidateAudience,
                ValidAudiences = validAudiences.Any() ? validAudiences : null,
                ValidateLifetime = _singleProviderOpts.ValidateLifetime,
                IssuerSigningKeys = allSigningKeys,
                ValidateIssuerSigningKey = true
            };

            var handler = new JwtSecurityTokenHandler();
            return handler.ValidateToken(idToken, validationParameters, out _);
        }

        /// <summary>
        /// Gets all valid issuers for a specific provider configuration.
        /// </summary>
        private static List<string> GetValidIssuersForProvider(OpenIdConnectOptions opts)
        {
            var issuers = new List<string>();

            if (!string.IsNullOrEmpty(opts.Issuer))
                issuers.Add(opts.Issuer);

            if (!string.IsNullOrEmpty(opts.Authority))
            {
                issuers.Add(opts.Authority);
                // Also add v2.0 variant if not already included
                var v2Issuer = $"{opts.Authority.TrimEnd('/')}/v2.0";
                if (!issuers.Contains(v2Issuer, StringComparer.OrdinalIgnoreCase))
                    issuers.Add(v2Issuer);
            }

            if (opts.ValidIssuers != null)
                issuers.AddRange(opts.ValidIssuers.Where(i => !string.IsNullOrEmpty(i)));

            return issuers.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// Gets all valid audiences for a specific provider configuration.
        /// </summary>
        private static List<string> GetValidAudiencesForProvider(OpenIdConnectOptions opts)
        {
            var audiences = new List<string>();

            if (!string.IsNullOrEmpty(opts.ClientId))
                audiences.Add(opts.ClientId);

            if (opts.ValidAudiences != null)
                audiences.AddRange(opts.ValidAudiences.Where(a => !string.IsNullOrEmpty(a)));

            return audiences.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// Validates a test ID token using the configured test authentication options.
        /// </summary>
        public ClaimsPrincipal ValidateTestIdToken(string idToken, bool cypressRequest = false)
        {
            return ValidateTestIdToken(idToken, cypressRequest, testOpts: null);
        }

        /// <summary>
        /// Validates a test ID token using the provided test authentication options.
        /// When <paramref name="testOpts"/> is supplied it takes precedence over the DI-registered defaults.
        /// </summary>
        public ClaimsPrincipal ValidateTestIdToken(string idToken, bool cypressRequest, TestAuthenticationOptions? testOpts)
        {
            if (string.IsNullOrWhiteSpace(idToken))
                throw new ArgumentNullException(nameof(idToken));

            var opts = testOpts ?? _testOpts;

            if (opts == null || (!opts.Enabled && !cypressRequest))
                throw new InvalidOperationException("Test authentication is not enabled or configured.");

            if (string.IsNullOrWhiteSpace(opts.JwtSigningKey))
                throw new InvalidOperationException("Test JWT signing key is not configured.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opts.JwtSigningKey));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = opts.ValidateIssuer,
                ValidIssuer = opts.ValidateIssuer ? opts.JwtIssuer : null,
                ValidateAudience = opts.ValidateAudience,
                ValidAudience = opts.ValidateAudience ? opts.JwtAudience : null,
                ValidateLifetime = opts.ValidateLifetime,
                IssuerSigningKey = key,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            var handler = new JwtSecurityTokenHandler();

            try
            {
                return handler.ValidateToken(idToken, validationParameters, out _);
            }
            catch (Exception ex)
            {
                throw new SecurityTokenException($"Test token validation failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validates an Internal Auth Id token using the provided or configured authentication options.
        /// </summary>
        /// <param name="idToken">The ID token to validate.</param>
        /// <param name="options">
        /// Optional internal auth options. If null, uses the configured defaults.
        /// </param>
        public ClaimsPrincipal ValidateInternalAuthToken(string idToken, InternalServiceAuthOptions? options = null)
        {
            if (string.IsNullOrWhiteSpace(idToken))
                throw new ArgumentNullException(nameof(idToken));

            var effectiveOptions = options ?? _internalAuthOpts;

            if (string.IsNullOrWhiteSpace(effectiveOptions?.SecretKey))
                throw new InvalidOperationException("Internal Auth signing key is not configured.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(effectiveOptions.SecretKey));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = effectiveOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = effectiveOptions.Audience,
                IssuerSigningKey = key,
                ValidateIssuerSigningKey = true,
            };

            var handler = new JwtSecurityTokenHandler();

            try
            {
                return handler.ValidateToken(idToken, validationParameters, out _);
            }
            catch (Exception ex)
            {
                throw new SecurityTokenException($"Internal Auth token validation failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Determines whether test authentication is enabled and configured.
        /// </summary>
        public bool IsTestAuthenticationEnabled => _testOpts?.Enabled == true;

        /// <summary>
        /// Indicates whether the validator is running in multi-provider mode.
        /// </summary>
        public bool IsMultiProviderMode => _isMultiProviderMode;

        /// <summary>
        /// Gets the number of configured providers.
        /// </summary>
        public int ProviderCount => _providers.Count;

        /// <summary>
        /// Disposes all internal configuration managers.
        /// </summary>
        public void Dispose()
        {
            foreach (var provider in _providers)
            {
                provider.Dispose();
            }
            _providers.Clear();
        }
    }
}
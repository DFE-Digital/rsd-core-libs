using DfE.CoreLibs.Security.Configurations;
using DfE.CoreLibs.Security.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace DfE.CoreLibs.Security.OpenIdConnect
{
    /// <summary>
    /// An implementation of <see cref="IExternalIdentityValidator"/> that uses the
    /// Microsoft.IdentityModel.Protocols stack to retrieve metadata and signing keys
    /// from an OpenID Connect provider, caching them automatically.
    /// </summary>
    public class ExternalIdentityValidator
        : IExternalIdentityValidator, IDisposable
    {
        private readonly ConfigurationManager<OpenIdConnectConfiguration> _configManager;
        private readonly OpenIdConnectOptions _opts;

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
        public ExternalIdentityValidator(
            IOptions<OpenIdConnectOptions> options,
            IHttpClientFactory httpClientFactory)
        {
            _opts = options?.Value
                ?? throw new ArgumentNullException(nameof(options));

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
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(idToken))
                throw new ArgumentNullException(nameof(idToken));

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

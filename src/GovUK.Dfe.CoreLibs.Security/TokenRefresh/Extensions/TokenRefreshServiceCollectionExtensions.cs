using GovUK.Dfe.CoreLibs.Security.TokenRefresh.Configuration;
using GovUK.Dfe.CoreLibs.Security.TokenRefresh.Interfaces;
using GovUK.Dfe.CoreLibs.Security.TokenRefresh.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GovUK.Dfe.CoreLibs.Security.TokenRefresh.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="IServiceCollection"/> to register token refresh services.
    /// </summary>
    public static class TokenRefreshServiceCollectionExtensions
    {
        /// <summary>
        /// Adds token refresh services to the specified <see cref="IServiceCollection"/> using the default provider.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="configuration">The application configuration containing token refresh settings.</param>
        /// <param name="sectionName">The configuration section name containing token refresh settings (defaults to "TokenRefresh").</param>
        /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when services or configuration is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the configuration section is missing or invalid.</exception>
        public static IServiceCollection AddTokenRefresh(
            this IServiceCollection services,
            IConfiguration configuration,
            string sectionName = "TokenRefresh")
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            return AddTokenRefresh(services, configuration, sectionName, null);
        }

        /// <summary>
        /// Adds token refresh services to the specified <see cref="IServiceCollection"/> using the default provider
        /// with custom configuration action.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="configuration">The application configuration containing token refresh settings.</param>
        /// <param name="sectionName">The configuration section name containing token refresh settings.</param>
        /// <param name="configureOptions">An optional action to configure the token refresh options.</param>
        /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when services or configuration is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the configuration section is missing or invalid.</exception>
        public static IServiceCollection AddTokenRefresh(
            this IServiceCollection services,
            IConfiguration configuration,
            string sectionName,
            Action<TokenRefreshOptions>? configureOptions)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            // Configure options
            var optionsBuilder = services.Configure<TokenRefreshOptions>(configuration.GetSection(sectionName));
            
            if (configureOptions != null)
            {
                optionsBuilder.PostConfigure<TokenRefreshOptions>(configureOptions);
            }

            // Validate configuration
            optionsBuilder.PostConfigure<TokenRefreshOptions>(options => options.Validate());

            // Register core services
            return AddTokenRefreshCore(services);
        }

        /// <summary>
        /// Adds token refresh services to the specified <see cref="IServiceCollection"/> with manual configuration.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="configureOptions">An action to configure the token refresh options.</param>
        /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when services or configureOptions is null.</exception>
        public static IServiceCollection AddTokenRefresh(
            this IServiceCollection services,
            Action<TokenRefreshOptions> configureOptions)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configureOptions == null)
                throw new ArgumentNullException(nameof(configureOptions));

            // Configure options
            services.Configure(configureOptions);
            services.PostConfigure<TokenRefreshOptions>(options => options.Validate());

            // Register core services
            return AddTokenRefreshCore(services);
        }

        /// <summary>
        /// Adds token refresh services to the specified <see cref="IServiceCollection"/> using a custom provider.
        /// </summary>
        /// <typeparam name="TProvider">The type of the custom token refresh provider.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="configuration">The application configuration containing token refresh settings.</param>
        /// <param name="sectionName">The configuration section name containing token refresh settings (defaults to "TokenRefresh").</param>
        /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when services or configuration is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the configuration section is missing or invalid.</exception>
        public static IServiceCollection AddTokenRefresh<TProvider>(
            this IServiceCollection services,
            IConfiguration configuration,
            string sectionName = "TokenRefresh")
            where TProvider : class, ITokenRefreshProvider
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            // Configure options
            services.Configure<TokenRefreshOptions>(configuration.GetSection(sectionName));
            services.PostConfigure<TokenRefreshOptions>(options => options.Validate());

            // Register core services with custom provider
            return AddTokenRefreshCore<TProvider>(services);
        }

        /// <summary>
        /// Adds token refresh services to the specified <see cref="IServiceCollection"/> using a custom provider
        /// with manual configuration.
        /// </summary>
        /// <typeparam name="TProvider">The type of the custom token refresh provider.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="configureOptions">An action to configure the token refresh options.</param>
        /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when services or configureOptions is null.</exception>
        public static IServiceCollection AddTokenRefresh<TProvider>(
            this IServiceCollection services,
            Action<TokenRefreshOptions> configureOptions)
            where TProvider : class, ITokenRefreshProvider
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configureOptions == null)
                throw new ArgumentNullException(nameof(configureOptions));

            // Configure options
            services.Configure(configureOptions);
            services.PostConfigure<TokenRefreshOptions>(options => options.Validate());

            // Register core services with custom provider
            return AddTokenRefreshCore<TProvider>(services);
        }

        /// <summary>
        /// Adds token refresh services with the default provider implementation.
        /// </summary>
        /// <param name="services">The service collection to add to.</param>
        /// <returns>The updated service collection.</returns>
        private static IServiceCollection AddTokenRefreshCore(IServiceCollection services)
        {
            return AddTokenRefreshCore<DefaultTokenRefreshProvider>(services);
        }

        /// <summary>
        /// Adds token refresh services with a custom provider implementation.
        /// </summary>
        /// <typeparam name="TProvider">The type of the token refresh provider.</typeparam>
        /// <param name="services">The service collection to add to.</param>
        /// <returns>The updated service collection.</returns>
        private static IServiceCollection AddTokenRefreshCore<TProvider>(IServiceCollection services)
            where TProvider : class, ITokenRefreshProvider
        {
            // Ensure HttpClient is available
            services.AddHttpClient();

            // Register services
            services.AddScoped<ITokenRefreshProvider, TProvider>();
            services.AddScoped<ITokenIntrospectionService, TokenIntrospectionService>();
            services.AddScoped<ITokenRefreshService, TokenRefreshService>();

            return services;
        }

        /// <summary>
        /// Adds token refresh services that integrate with existing OpenID Connect configuration.
        /// This method extends the existing OpenID Connect setup to include token refresh capabilities.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="oidcSectionName">The OpenID Connect configuration section name (defaults to "DfESignIn").</param>
        /// <param name="tokenRefreshSectionName">The token refresh configuration section name (defaults to "TokenRefresh").</param>
        /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when services or configuration is null.</exception>
        public static IServiceCollection AddTokenRefreshWithOidc(
            this IServiceCollection services,
            IConfiguration configuration,
            string oidcSectionName = "DfESignIn",
            string tokenRefreshSectionName = "TokenRefresh")
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            // Configure token refresh options, optionally inheriting from OIDC configuration
            services.Configure<TokenRefreshOptions>(tokenRefreshConfiguration =>
            {
                // Bind from token refresh section
                configuration.GetSection(tokenRefreshSectionName).Bind(tokenRefreshConfiguration);

                // If token refresh specific endpoints are not configured, try to inherit from OIDC
                var oidcSection = configuration.GetSection(oidcSectionName);
                if (oidcSection.Exists())
                {
                    var authority = oidcSection["Authority"];
                    var clientId = oidcSection["ClientId"];
                    var clientSecret = oidcSection["ClientSecret"];

                    // Set defaults from OIDC if token refresh values are not specified
                    if (string.IsNullOrWhiteSpace(tokenRefreshConfiguration.ClientId) && !string.IsNullOrWhiteSpace(clientId))
                    {
                        tokenRefreshConfiguration.ClientId = clientId;
                    }

                    if (string.IsNullOrWhiteSpace(tokenRefreshConfiguration.ClientSecret) && !string.IsNullOrWhiteSpace(clientSecret))
                    {
                        tokenRefreshConfiguration.ClientSecret = clientSecret;
                    }

                    // Construct standard endpoints if not explicitly configured
                    if (!string.IsNullOrWhiteSpace(authority))
                    {
                        var authorityUri = authority.TrimEnd('/');
                        
                        if (string.IsNullOrWhiteSpace(tokenRefreshConfiguration.TokenEndpoint))
                        {
                            tokenRefreshConfiguration.TokenEndpoint = $"{authorityUri}/token";
                        }

                        if (string.IsNullOrWhiteSpace(tokenRefreshConfiguration.IntrospectionEndpoint))
                        {
                            tokenRefreshConfiguration.IntrospectionEndpoint = $"{authorityUri}/introspect";
                        }
                    }
                }
            });

            services.PostConfigure<TokenRefreshOptions>(options => options.Validate());

            // Register core services
            return AddTokenRefreshCore(services);
        }
    }
}

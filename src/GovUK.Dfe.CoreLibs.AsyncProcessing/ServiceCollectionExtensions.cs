using GovUK.Dfe.CoreLibs.AsyncProcessing.Configurations;
using GovUK.Dfe.CoreLibs.AsyncProcessing.Interfaces;
using GovUK.Dfe.CoreLibs.AsyncProcessing.Services;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the BackgroundServiceFactory with optional configuration.
        /// Note: Please ensure mediatr is registered.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configureOptions">An optional action to configure BackgroundServiceOptions</param>
        public static IServiceCollection AddBackgroundService(
            this IServiceCollection services,
            Action<BackgroundServiceOptions>? configureOptions = null)
        {
            // If no configuration delegate is provided, use defaults
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            else
            {
                services.Configure<BackgroundServiceOptions>(_ => { });
            }

            services.AddSingleton<IBackgroundServiceFactory, BackgroundServiceFactory>();
            services.AddHostedService<BackgroundServiceFactory>();

            return services;
        }

    }
}

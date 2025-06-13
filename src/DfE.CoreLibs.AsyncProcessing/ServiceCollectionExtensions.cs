using DfE.CoreLibs.AsyncProcessing.Configurations;
using DfE.CoreLibs.AsyncProcessing.Interfaces;
using DfE.CoreLibs.AsyncProcessing.Services;

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
                services.AddOptions<BackgroundServiceOptions>()
                    .Configure(configureOptions)
                    .ValidateDataAnnotations()
                    .ValidateOnStart();
            }
            else
            {
                services.AddOptions<BackgroundServiceOptions>()
                    .Configure(_ => { })
                    .ValidateOnStart();
            }

            services.AddSingleton<IBackgroundServiceFactory, BackgroundServiceFactory>();
            services.AddHostedService<BackgroundServiceFactory>();

            return services;
        }

    }
}
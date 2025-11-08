using GovUK.Dfe.CoreLibs.AsyncProcessing.Configurations;
using GovUK.Dfe.CoreLibs.AsyncProcessing.Interfaces;
using GovUK.Dfe.CoreLibs.AsyncProcessing.Services;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the channel-based BackgroundServiceFactory with optional configuration.
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configureOptions">An optional action to configure BackgroundServiceOptions</param>
        public static IServiceCollection AddBackgroundService(
            this IServiceCollection services,
            Action<BackgroundServiceOptions>? configureOptions = null)
        {
            // Configure options
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            else
            {
                services.Configure<BackgroundServiceOptions>(_ => { });
            }

            // Register as both singleton factory and hosted service
            services.AddSingleton<IBackgroundServiceFactory, BackgroundServiceFactory>();
            services.AddHostedService(sp => (BackgroundServiceFactory)sp.GetRequiredService<IBackgroundServiceFactory>());

            return services;
        }

        /// <summary>
        /// Registers BackgroundServiceFactory with parallel processing capability.
        /// This is a convenience method for common parallel processing scenarios.
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="maxConcurrentWorkers">Number of concurrent worker tasks (default: 4)</param>
        /// <param name="channelCapacity">Optional channel capacity limit. Null means unbounded.</param>
        public static IServiceCollection AddBackgroundServiceWithParallelism(
            this IServiceCollection services,
            int maxConcurrentWorkers = 4,
            int? channelCapacity = null)
        {
            return services.AddBackgroundService(options =>
            {
                options.MaxConcurrentWorkers = maxConcurrentWorkers;
                options.ChannelCapacity = channelCapacity ?? int.MaxValue;
                options.UseGlobalStoppingToken = true;
            });
        }
    }
}

using DfE.CoreLibs.AsyncProcessing.Interfaces;
using DfE.CoreLibs.AsyncProcessing.Services;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBackgroundService(this IServiceCollection services)
        {
            services.AddSingleton<IBackgroundServiceFactory, BackgroundServiceFactory>();
            services.AddHostedService<BackgroundServiceFactory>();

            return services;
        }
    }
}
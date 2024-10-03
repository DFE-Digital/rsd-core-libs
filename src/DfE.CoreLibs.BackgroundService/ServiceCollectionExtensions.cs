using DfE.CoreLibs.BackgroundService.Interfaces;
using DfE.CoreLibs.BackgroundService.Services;

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

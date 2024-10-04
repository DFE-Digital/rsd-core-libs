using DfE.CoreLibs.Caching.Interfaces;
using DfE.CoreLibs.Caching.Services;
using DfE.CoreLibs.Caching.Settings;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddServiceCaching(
            this IServiceCollection services, IConfiguration config)
        {
            services.Configure<CacheSettings>(config.GetSection("CacheSettings"));
            services.AddSingleton<ICacheService, MemoryCacheService>();

            return services;
        }
    }
}

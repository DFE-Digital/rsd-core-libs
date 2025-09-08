using GovUK.Dfe.CoreLibs.Caching.Interfaces;
using GovUK.Dfe.CoreLibs.Caching.Services;
using GovUK.Dfe.CoreLibs.Caching.Settings;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddServiceCaching(
            this IServiceCollection services, IConfiguration config)
        {
            services.Configure<CacheSettings>(config.GetSection("CacheSettings"));
            services.AddSingleton<ICacheService<IMemoryCacheType>, MemoryCacheService>();

            return services;
        }
    }
}

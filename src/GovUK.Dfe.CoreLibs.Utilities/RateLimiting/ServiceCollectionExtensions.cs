using Microsoft.Extensions.DependencyInjection;

namespace GovUK.Dfe.CoreLibs.Utilities.RateLimiting
{
    /// <summary>
    /// Extension methods to register rate limiting services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the rate limit store and factory in the DI container.
        /// </summary>
        public static IServiceCollection AddRateLimiting<TKey>(this IServiceCollection services)
            where TKey : notnull
        {
            services.AddSingleton<RateLimitStore<TKey>>();
            services.AddSingleton<IRateLimiterFactory<TKey>, RateLimiterFactory<TKey>>();
            return services;
        }
    }
}

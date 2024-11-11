using DfE.CoreLibs.Security.Authorization;
using DfE.CoreLibs.Security.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace DfE.CoreLibs.Security
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the Token Service and its dependencies in the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="configuration">The application configuration containing the role-to-scope mappings.</param>
        /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddTokenService(this IServiceCollection services)
        {
            services.AddSingleton<IApiTokenService, ApiTokenService>();
            services.AddHttpContextAccessor();
            return services;
        }
    }
}

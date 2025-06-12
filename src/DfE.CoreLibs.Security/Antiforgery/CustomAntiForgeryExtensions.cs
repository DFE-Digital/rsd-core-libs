using DfE.CoreLibs.Security.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace DfE.CoreLibs.Security.Antiforgery
{
    /// <summary>
    /// Provides extension methods for registering AntiForgery handling in the MVC pipeline.
    /// </summary>
    public static class CustomAntiForgeryExtensions
    {
        /// <summary>  
        /// Registers the <see cref="ICustomRequestChecker"/> and <see cref="CustomAwareAntiForgeryFilter"/> services,  
        /// and inserts the custom AntiForgery filter globally into the MVC pipeline.  
        /// </summary>  
        /// <param name="mvcBuilder">The MVC builder.</param>
        /// <returns>The same <see cref="IMvcBuilder"/> so further MVC configuration can be chained.</returns>  
        public static IMvcBuilder AddCustomAntiForgeryHandling(this IMvcBuilder mvcBuilder)
        {
            ArgumentNullException.ThrowIfNull(mvcBuilder);

            // Register the filter as a service, letting DI resolve skipConditions
            mvcBuilder.Services.AddScoped<CustomAwareAntiForgeryFilter>();

            mvcBuilder.Services.PostConfigure<MvcOptions>(options =>
            {
                options.Filters.AddService<CustomAwareAntiForgeryFilter>();
            });

            return mvcBuilder;
        }

        public static IServiceCollection AddCustomRequestCheckerProvider<TProvider>(this IServiceCollection services)
            where TProvider : class, ICustomRequestChecker
        {
            services.AddTransient<ICustomRequestChecker, TProvider>();
            return services;
        }
    }
}

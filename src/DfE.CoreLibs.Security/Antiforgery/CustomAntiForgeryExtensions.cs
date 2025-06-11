using DfE.CoreLibs.Security.Cypress;
using DfE.CoreLibs.Security.Interfaces;
using Microsoft.AspNetCore.Http;
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
        /// <param name="skipConditions">A list of conditions to skip AntiForgery validation.</param>  
        /// <returns>The same <see cref="IMvcBuilder"/> so further MVC configuration can be chained.</returns>  
        public static IMvcBuilder AddCustomAntiForgeryHandling(this IMvcBuilder mvcBuilder, List<Func<HttpContext, bool>> skipConditions)
        {
            ArgumentNullException.ThrowIfNull(mvcBuilder);
            ArgumentNullException.ThrowIfNull(skipConditions);

            // Register skipConditions as a singleton so it can be injected
            mvcBuilder.Services.AddSingleton(skipConditions);

            // Register the filter as a service, letting DI resolve skipConditions
            mvcBuilder.Services.AddScoped<CustomAwareAntiForgeryFilter>();

            mvcBuilder.Services.PostConfigure<MvcOptions>(options =>
            {
                options.Filters.AddService<CustomAwareAntiForgeryFilter>();
            });

            return mvcBuilder;
        }
    }
}

using DfE.CoreLibs.Security.Cypress;
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
        /// <remarks>
        /// <para>
        /// By calling this method, any incoming request recognized as a custom request 
        /// (per <see cref="ICustomRequestChecker"/>) will skip AntiForgery validation.
        /// Other requests will require AntiForgery validation as normal.
        /// </para>
        /// <para>
        /// By calling this method, any incoming request recognized as a Cypress request 
        /// (per <see cref="ICypressRequestChecker"/>) will skip AntiForgery validation.
        /// Other requests will require AntiForgery validation as normal.
        /// </para>
        /// </remarks>
        /// <returns>
        /// The same <see cref="IMvcBuilder"/> so further MVC configuration can be chained.
        /// </returns>
        public static IMvcBuilder AddCustomAntiForgeryHandling(this IMvcBuilder mvcBuilder)
        {
            mvcBuilder.Services.AddScoped<ICustomRequestChecker, CustomRequestChecker>();

            mvcBuilder.Services.AddScoped<ICypressRequestChecker, CypressRequestChecker>();

            mvcBuilder.Services.AddScoped<CustomAwareAntiForgeryFilter>();

            mvcBuilder.Services.PostConfigure<MvcOptions>(options =>
            {
                options.Filters.AddService<CustomAwareAntiForgeryFilter>();
            });

            return mvcBuilder;
        }
    }
}

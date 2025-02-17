using DfE.CoreLibs.Security.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace DfE.CoreLibs.Security.Cypress
{
    /// <summary>
    /// Provides extension methods for registering Cypress-related antiforgery handling in the MVC pipeline.
    /// </summary>
    public static class CypressAntiforgeryExtensions
    {
        /// <summary>
        /// Registers the <see cref="ICypressRequestChecker"/> and <see cref="CypressAwareAntiforgeryFilter"/> services,
        /// and inserts the Cypress antiforgery filter globally into the MVC pipeline.
        /// </summary>
        /// <remarks>
        /// <para>
        /// By calling this method, any incoming request recognized as a Cypress request 
        /// (per <see cref="ICypressRequestChecker"/>) will skip antiforgery validation.
        /// Other requests will require antiforgery validation as normal.
        /// </para>
        /// </remarks>
        /// <returns>
        /// The same <see cref="IMvcBuilder"/> so further MVC configuration can be chained.
        /// </returns>
        public static IMvcBuilder AddCypressAntiforgeryHandling(this IMvcBuilder mvcBuilder)
        {
            mvcBuilder.Services.AddScoped<ICypressRequestChecker, CypressRequestChecker>();

            mvcBuilder.Services.AddScoped<CypressAwareAntiforgeryFilter>();

            mvcBuilder.Services.PostConfigure<MvcOptions>(options =>
            {
                options.Filters.AddService<CypressAwareAntiforgeryFilter>();
            });

            return mvcBuilder;
        }
    }
}

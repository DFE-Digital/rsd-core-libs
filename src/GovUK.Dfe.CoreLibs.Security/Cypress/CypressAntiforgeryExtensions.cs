using GovUK.Dfe.CoreLibs.Security.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace GovUK.Dfe.CoreLibs.Security.Cypress
{
    /// <summary>
    /// Provides extension methods for registering Cypress-related AntiForgery handling in the MVC pipeline.
    /// </summary>
    public static class CypressAntiForgeryExtensions
    {
        /// <summary>
        /// Registers the <see cref="ICypressRequestChecker"/> and <see cref="CypressAwareAntiForgeryFilter"/> services,
        /// and inserts the Cypress AntiForgery filter globally into the MVC pipeline.
        /// </summary>
        /// <remarks>
        /// <para>
        /// By calling this method, any incoming request recognized as a Cypress request 
        /// (per <see cref="ICypressRequestChecker"/>) will skip AntiForgery validation.
        /// Other requests will require AntiForgery validation as normal.
        /// </para>
        /// </remarks>
        /// <returns>
        /// The same <see cref="IMvcBuilder"/> so further MVC configuration can be chained.
        /// </returns>
        public static IMvcBuilder AddCypressAntiForgeryHandling(this IMvcBuilder mvcBuilder)
        {
            mvcBuilder.Services.AddScoped<ICustomRequestChecker, CypressRequestChecker>();

            mvcBuilder.Services.AddScoped<CypressAwareAntiForgeryFilter>();

            mvcBuilder.Services.PostConfigure<MvcOptions>(options =>
            {
                options.Filters.AddService<CypressAwareAntiForgeryFilter>();
            });

            return mvcBuilder;
        }
    }
}

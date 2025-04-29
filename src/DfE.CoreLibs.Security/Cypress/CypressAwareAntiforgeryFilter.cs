using DfE.CoreLibs.Security.Interfaces;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DfE.CoreLibs.Security.Cypress
{
    /// <summary>
    /// An authorization filter that enforces AntiForgery validation for all requests,
    /// except for those recognized as valid Cypress requests or for which the
    /// configured predicate says to skip.
    /// </summary>
    public class CypressAwareAntiForgeryFilter(
        IAntiforgery antiforgery,
        ILogger<CypressAwareAntiForgeryFilter> logger,
        ICypressRequestChecker cypressChecker,
        IOptions<CypressAwareAntiForgeryOptions> optionsAccessor)
        : IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (optionsAccessor.Value.ShouldSkipAntiforgery(context.HttpContext))
            {
                logger.LogInformation("Skipping anti-forgery due to ShouldSkipAntiforgery predicate.");
                return;
            }

            var method = context.HttpContext.Request.Method;
            if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method) ||
                HttpMethods.IsOptions(method) || HttpMethods.IsTrace(method))
            {
                return;
            }

            var isCypress = cypressChecker.IsCypressRequest(context.HttpContext);
            if (isCypress)
            {
                logger.LogInformation("Skipping anti-forgery for Cypress request.");
                return;
            }

            logger.LogInformation("Enforcing anti-forgery for non-Cypress request.");
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
    }
}

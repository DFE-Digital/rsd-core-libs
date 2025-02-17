using DfE.CoreLibs.Security.Interfaces;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace DfE.CoreLibs.Security.Cypress
{
    /// <summary>
    /// An authorization filter that enforces antiforgery validation for all requests,
    /// except for those recognized as valid Cypress requests.
    /// </summary>
    public class CypressAwareAntiforgeryFilter(
        IAntiforgery antiforgery,
        ILogger<CypressAwareAntiforgeryFilter> logger,
        ICypressRequestChecker cypressChecker)
        : IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var method = context.HttpContext.Request.Method;
            if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method) ||
                HttpMethods.IsOptions(method) || HttpMethods.IsTrace(method))
            {
                return;
            }

            var isCypress = cypressChecker.IsCypressRequest(context.HttpContext);
            if (isCypress)
            {
                logger.LogInformation("Skipping antiforgery for Cypress request");
                return;
            }

            logger.LogInformation("Enforcing antiforgery for non-Cypress request");
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
    }

}

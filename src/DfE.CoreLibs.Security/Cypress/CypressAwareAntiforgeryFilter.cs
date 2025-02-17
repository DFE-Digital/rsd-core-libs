using DfE.CoreLibs.Security.Interfaces;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace DfE.CoreLibs.Security.Cypress
{
    /// <summary>
    /// An authorization filter that enforces AntiForgery validation for all requests,
    /// except for those recognized as valid Cypress requests.
    /// </summary>
    public class CypressAwareAntiForgeryFilter(
        IAntiforgery antiForgery,
        ILogger<CypressAwareAntiForgeryFilter> logger,
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
                logger.LogInformation("Skipping AntiForgery for Cypress request");
                return;
            }

            logger.LogInformation("Enforcing AntiForgery for non-Cypress request");
            await antiForgery.ValidateRequestAsync(context.HttpContext);
        }
    }

}

using DfE.CoreLibs.Security.Interfaces;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace DfE.CoreLibs.Security.Antiforgery
{
    /// <summary>
    /// An authorization filter that enforces AntiForgery validation for all requests,
    /// except for those recognized as valid custom or Cypress requests or for which the
    /// configured predicate says to skip.
    /// </summary>
    public class CustomAwareAntiForgeryFilter(
        IAntiforgery antiforgery,
        List<ICustomRequestChecker> customRequestCheckers,
        ILogger<CustomAwareAntiForgeryFilter> logger)
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
            var isValidRequests = customRequestCheckers.Select(checker => checker.IsValidRequest(context.HttpContext)).ToList();

            if (isValidRequests.Count == 0 || isValidRequests.Contains(false))
            {
                logger.LogInformation("Enforcing anti-forgery for the request.");
                await antiforgery.ValidateRequestAsync(context.HttpContext);
                
            }
            logger.LogInformation("Skipping anti-forgery for the request due to matching all conditions.");
        }
    }
}

using DfE.CoreLibs.Security.Enums;
using DfE.CoreLibs.Security.Interfaces;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DfE.CoreLibs.Security.Antiforgery
{
    public class CustomAwareAntiForgeryFilter(
        IAntiforgery antiforgery,
        IEnumerable<ICustomRequestChecker> customRequestCheckers,
        IOptions<CustomAwareAntiForgeryOptions> options,
        ILogger<CustomAwareAntiForgeryFilter> logger)
        : IAsyncAuthorizationFilter
    {
        private readonly List<CheckerGroup> _groups = options.Value.CheckerGroups;

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // skip safe methods
            var m = context.HttpContext.Request.Method;
            if (HttpMethods.IsGet(m) || HttpMethods.IsHead(m) ||
                HttpMethods.IsOptions(m) || HttpMethods.IsTrace(m))
                return;

            var groupResults = new List<bool>();

            foreach (var g in _groups)
            {
                var matchers = customRequestCheckers
                    .Where(c => g.TypeNames.Contains(c.GetType().Name))
                    .ToList();

                if (matchers.Count == 0)
                {
                    groupResults.Add(false);
                    continue;
                }

                var results = matchers
                    .Select(c => c.IsValidRequest(context.HttpContext));

                var groupPassed = g.CheckerOperator switch
                {
                    CheckerOperator.Or => results.Any(r => r),
                    CheckerOperator.And => results.All(r => r),
                    _ => false
                };

                logger.LogInformation("Group [{Types}] with {Op} => {Result}", string.Join(",", g.TypeNames),
                    g.CheckerOperator, groupPassed);

                groupResults.Add(groupPassed);
            }

            // only skip antiforgery if every group passed, this is the case if we have morte than one group of Checkers
            if (groupResults.Count > 0 && groupResults.All(r => r))
            {
                logger.LogInformation("All groups passed > skipping antiforgery.");
                return;
            }

            logger.LogInformation("Enforcing anti-forgery for the request.");
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
    }
}
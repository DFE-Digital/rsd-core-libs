using GovUK.Dfe.CoreLibs.Security.Authorization.Requirements;
using Microsoft.AspNetCore.Authorization;

namespace GovUK.Dfe.CoreLibs.Security.Authorization.Handlers
{
    public class ResourcePermissionHandler
        : AuthorizationHandler<ResourcePermissionRequirement, string>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ResourcePermissionRequirement requirement,
            string resourceKey)
        {
            if (string.IsNullOrWhiteSpace(resourceKey))
                return Task.CompletedTask;

            var expected = $"{resourceKey}:{requirement.Action}";

            if (context.User.Claims.Any(c =>
                    c.Type == requirement.ClaimType
                    && c.Value == expected))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}

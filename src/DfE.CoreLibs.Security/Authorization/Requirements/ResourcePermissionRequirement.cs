using Microsoft.AspNetCore.Authorization;

namespace DfE.CoreLibs.Security.Authorization.Requirements
{
    /// <summary>
    /// A dynamic requirement: at runtime we’ll be handed a resourceKey,
    /// and must check for a claim "{resourceKey}:{Action}" of the given ClaimType.
    /// </summary>
    public class ResourcePermissionRequirement(string action, string claimType) : IAuthorizationRequirement
    {
        public string Action { get; } = action ?? throw new ArgumentNullException(nameof(action));
        public string ClaimType { get; } = claimType ?? throw new ArgumentNullException(nameof(claimType));
    }
}

using System.Security.Claims;
using GovUK.Dfe.CoreLibs.Security.Extensions;

namespace GovUK.Dfe.CoreLibs.Security.Interfaces
{
    /// <summary>
    /// A facade over <see cref="ClaimsPrincipal"/> exposing the current user
    /// and their resource-based permissions.
    /// </summary>
    public interface ICurrentUser
    {
        /// <summary>
        /// The unique identifier of the current user.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// The display name of the current user (if available).
        /// </summary>
        string? Name { get; }

        /// <summary>
        /// Returns true if the current user has the specified action
        /// on the given resource, via a resource-action claim.
        /// </summary>
        /// <param name="resourceKey">The resource identifier.</param>
        /// <param name="action">The action name to check.</param>
        /// <param name="claimType">
        ///   Optional claim type; defaults to
        ///   <see cref="PermissionExtensions.DefaultPermissionClaimType"/>.
        /// </param>
        bool HasPermission(string resourceKey, string action, string claimType = PermissionExtensions.DefaultPermissionClaimType);
    }
}

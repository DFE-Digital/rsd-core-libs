using DfE.CoreLibs.Security.Extensions;
using System.Security.Claims;

namespace DfE.CoreLibs.Security.Interfaces
{
    /// <summary>
    /// Extended user context exposing additional details about the current user.
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

        /// <summary>
        /// The email address of the current user (if available).
        /// </summary>
        string? Email { get; }

        /// <summary>
        /// Returns true if the current user is authenticated.
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// The underlying <see cref="ClaimsPrincipal"/> for advanced scenarios.
        /// </summary>
        ClaimsPrincipal Principal { get; }

        /// <summary>
        /// All claims belonging to the user.
        /// </summary>
        IEnumerable<Claim> Claims { get; }

        /// <summary>
        /// Retrieves the value for a given claim type if present.
        /// </summary>
        /// <param name="claimType">The claim type.</param>
        /// <returns>The claim value or <c>null</c> if missing.</returns>
        string? GetClaimValue(string claimType);
    }
}

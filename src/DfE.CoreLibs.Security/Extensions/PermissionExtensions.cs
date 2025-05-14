using System.Security.Claims;

namespace DfE.CoreLibs.Security.Extensions
{
    /// <summary>
    /// Provides extension methods for checking resource-based permissions
    /// on a <see cref="ClaimsPrincipal"/>.
    /// </summary>
    public static class PermissionExtensions
    {
        /// <summary>
        /// The default claim type under which resource-action claims are stored.
        /// </summary>
        public const string DefaultPermissionClaimType = "permission";

        /// <summary>
        /// Returns <c>true</c> if the specified <paramref name="user"/> has a claim of type
        /// <paramref name="claimType"/> whose value equals
        /// <c>&lt;resourceKey&gt;:&lt;action&gt;</c>.
        /// </summary>
        /// <param name="user">The current user principal.</param>
        /// <param name="resourceKey">
        ///   A unique identifier for the protected resource (e.g. a TaskId, PageKey, etc.).
        /// </param>
        /// <param name="action">
        ///   The action to verify (e.g. "Read", "Write", "Delete", etc.).
        /// </param>
        /// <param name="claimType">
        ///   The claim type that holds your resource-action values.
        ///   Defaults to <see cref="DefaultPermissionClaimType"/>.
        /// </param>
        /// <returns>
        ///   <c>true</c> if the user has a matching "<paramref name="resourceKey"/>:<paramref name="action"/>"
        ///   claim; otherwise <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   Thrown if <paramref name="user"/>, <paramref name="resourceKey"/>, or <paramref name="action"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   Thrown if <paramref name="resourceKey"/> or <paramref name="action"/> is empty or whitespace.
        /// </exception>
        public static bool HasPermission(
            this ClaimsPrincipal user,
            string resourceKey,
            string action,
            string claimType = DefaultPermissionClaimType)
        {
            ArgumentNullException.ThrowIfNull(user);
            if (string.IsNullOrWhiteSpace(resourceKey))
                throw new ArgumentException("Resource key must be provided", nameof(resourceKey));
            if (string.IsNullOrWhiteSpace(action))
                throw new ArgumentException("Action must be provided", nameof(action));

            var requiredValue = $"{resourceKey}:{action}";
            return user.Claims
                       .Where(c => string.Equals(c.Type, claimType, StringComparison.OrdinalIgnoreCase))
                       .Any(c => string.Equals(c.Value, requiredValue, StringComparison.OrdinalIgnoreCase));
        }
    }
}
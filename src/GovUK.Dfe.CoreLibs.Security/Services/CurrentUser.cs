using GovUK.Dfe.CoreLibs.Security.Extensions;
using GovUK.Dfe.CoreLibs.Security.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace GovUK.Dfe.CoreLibs.Security.Services
{
    /// <summary>
    /// implementation of <see cref="ICurrentUser"/> that reads from
    /// <see cref="HttpContext.User"/>.
    /// </summary>
    public class CurrentUser : ICurrentUser
    {
        private readonly ClaimsPrincipal _user;

        /// <summary>
        /// Constructs a new <see cref="CurrentUser"/> by retrieving
        /// the <see cref="HttpContext.User"/> from the provided accessor.
        /// </summary>
        /// <param name="httpContextAccessor">Gives access to the current <see cref="HttpContext"/>.</param>
        /// <exception cref="InvalidOperationException">If no <see cref="HttpContext"/> is available.</exception>
        public CurrentUser(IHttpContextAccessor httpContextAccessor)
        {
            _user = httpContextAccessor.HttpContext?.User
                    ?? throw new InvalidOperationException("No HttpContext available to resolve the current user.");
        }

        /// <inheritdoc/>
        public string Id =>
            _user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User identity does not contain a NameIdentifier claim.");

        /// <inheritdoc/>
        public string? Name =>
            _user.Identity?.Name;

        /// <inheritdoc/>
        public bool HasPermission(string resourceKey, string action, string claimType = PermissionExtensions.DefaultPermissionClaimType)
        {
            // Leverage the ClaimsPrincipal extension for the actual check
            return _user.HasPermission(resourceKey, action, claimType);
        }
    }
}

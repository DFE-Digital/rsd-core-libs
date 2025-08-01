using DfE.CoreLibs.Security.Extensions;
using DfE.CoreLibs.Security.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace DfE.CoreLibs.Security.Services;

/// <summary>
/// Implementation of <see cref="ICurrentUser"/> that reads from
/// the <see cref="HttpContext.User"/>.
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

    /// <inheritdoc />
    public string Id =>
        _user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("User identity does not contain a NameIdentifier claim.");

    /// <inheritdoc />
    public string? Name => _user.Identity?.Name;

    /// <inheritdoc />
    public string? Email => _user.FindFirstValue(ClaimTypes.Email);

    /// <inheritdoc />
    public bool IsAuthenticated => _user.Identity?.IsAuthenticated ?? false;

    /// <inheritdoc />
    public ClaimsPrincipal Principal => _user;

    /// <inheritdoc />
    public IEnumerable<Claim> Claims => _user.Claims;

    /// <inheritdoc />
    public bool HasPermission(string resourceKey, string action, string claimType = PermissionExtensions.DefaultPermissionClaimType)
    {
        // Leverage the ClaimsPrincipal extension for the actual check
        return _user.HasPermission(resourceKey, action, claimType);
    }

    /// <inheritdoc />
    public string? GetClaimValue(string claimType)
    {
        return _user.FindFirstValue(claimType);
    }
}



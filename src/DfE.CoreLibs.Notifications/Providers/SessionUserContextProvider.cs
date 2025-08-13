using DfE.CoreLibs.Notifications.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace DfE.CoreLibs.Notifications.Providers;

/// <summary>
/// User context provider that uses HTTP session for identification
/// </summary>
public class SessionUserContextProvider : IUserContextProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the SessionUserContextProvider
    /// </summary>
    /// <param name="httpContextAccessor">HTTP context accessor</param>
    public SessionUserContextProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <summary>
    /// Gets the current user ID, preferring:
    /// 1) NameIdentifier (or "sub"), 2) Email, 3) Identity.Name. Falls back to "default".
    /// </summary>
    public string GetCurrentUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;

        if (user?.Identity?.IsAuthenticated != true)
            return "default";

        var id =
            user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value
            ?? user.FindFirst(ClaimTypes.Email)?.Value
            ?? user.Identity?.Name;

        return string.IsNullOrWhiteSpace(id) ? "default" : id;
    }

    /// <summary>
    /// True if any of Identifier / Email / Name is available on the current principal.
    /// </summary>
    public bool IsContextAvailable()
    {
        var user = _httpContextAccessor.HttpContext?.User;

        if (user?.Identity?.IsAuthenticated != true)
            return false;

        return
            !string.IsNullOrWhiteSpace(user.FindFirst(ClaimTypes.NameIdentifier)?.Value) ||
            !string.IsNullOrWhiteSpace(user.FindFirst("sub")?.Value) ||
            !string.IsNullOrWhiteSpace(user.FindFirst(ClaimTypes.Email)?.Value) ||
            !string.IsNullOrWhiteSpace(user.Identity?.Name);
    }
}
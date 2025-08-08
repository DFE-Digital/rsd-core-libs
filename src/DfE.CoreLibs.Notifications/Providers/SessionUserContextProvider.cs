using DfE.CoreLibs.Notifications.Interfaces;
using Microsoft.AspNetCore.Http;

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
    /// Get the current user ID from the HTTP context user identity
    /// </summary>
    /// <returns>User name from identity or "default" if not available</returns>
    public string GetCurrentUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.Name != null)
        {
            return httpContext.User.Identity.Name;
        }

        return "default";
    }

    /// <summary>
    /// Check if user context is available
    /// </summary>
    /// <returns>True if user identity with name is available, false otherwise</returns>
    public bool IsContextAvailable()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        return httpContext?.User?.Identity?.Name != null;
    }
}
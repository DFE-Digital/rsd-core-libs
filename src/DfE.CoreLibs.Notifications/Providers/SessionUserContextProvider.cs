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
    /// Get the current session ID as the user identifier
    /// </summary>
    /// <returns>Session ID</returns>
    /// <exception cref="InvalidOperationException">Thrown when session is not available</exception>
    public string GetCurrentUserId()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        if (session == null)
            throw new InvalidOperationException("Session is not available");

        return session.Id;
    }

    /// <summary>
    /// Check if session context is available
    /// </summary>
    /// <returns>True if session is available, false otherwise</returns>
    public bool IsContextAvailable()
    {
        return _httpContextAccessor.HttpContext?.Session != null;
    }
}
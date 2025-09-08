namespace GovUK.Dfe.CoreLibs.Notifications.Interfaces;

/// <summary>
/// Provides user context for notification operations
/// </summary>
public interface IUserContextProvider
{
    /// <summary>
    /// Get the current user identifier
    /// This could be a session ID, user ID, or any other identifier depending on the implementation
    /// </summary>
    /// <returns>User identifier</returns>
    string GetCurrentUserId();

    /// <summary>
    /// Check if a user context is available
    /// </summary>
    /// <returns>True if context is available, false otherwise</returns>
    bool IsContextAvailable();
}

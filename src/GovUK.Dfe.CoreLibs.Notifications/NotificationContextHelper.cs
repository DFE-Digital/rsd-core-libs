namespace GovUK.Dfe.CoreLibs.Notifications;

/// <summary>
/// Shared rules for notification context scoping and deduplication.
/// Context values may be scoped as "{tenantApp}|{detail}" so list queries can filter by prefix.
/// </summary>
public static class NotificationContextHelper
{
    public const char ScopeSeparator = '|';

    /// <summary>
    /// True when <paramref name="notificationContext"/> belongs to <paramref name="scopeContext"/>.
    /// Exact match or "{scopeContext}|…" prefix match.
    /// </summary>
    public static bool BelongsToScope(string? notificationContext, string? scopeContext)
    {
        if (string.IsNullOrEmpty(scopeContext))
            return true;

        if (string.IsNullOrEmpty(notificationContext))
            return false;

        return notificationContext.Equals(scopeContext, StringComparison.Ordinal)
               || notificationContext.StartsWith(scopeContext + ScopeSeparator, StringComparison.Ordinal);
    }

    /// <summary>
    /// Builds a scoped context for storage, e.g. "Transfers|file-upload|{fileId}".
    /// </summary>
    public static string BuildScopedContext(string scopeContext, params string?[] parts)
    {
        var segments = new List<string> { scopeContext.Trim() };
        foreach (var part in parts)
        {
            if (!string.IsNullOrWhiteSpace(part))
                segments.Add(part.Trim());
        }

        return string.Join(ScopeSeparator, segments);
    }
}

namespace GovUK.Dfe.CoreLibs.ApplicationSettings.Interfaces;

public interface IApplicationSettingsService
{
    /// <summary>
    /// Get a setting value by key
    /// </summary>
    Task<string?> GetSettingAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a strongly-typed setting value by key
    /// </summary>
    Task<T?> GetSettingAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Get a setting value by key with a default value if not found
    /// </summary>
    Task<string> GetSettingAsync(string key, string defaultValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all settings for a specific category
    /// </summary>
    Task<Dictionary<string, string>> GetSettingsByCategoryAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active settings
    /// </summary>
    Task<Dictionary<string, string>> GetAllSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Set a setting value
    /// </summary>
    Task SetSettingAsync(string key, string value, string? description = null, string? category = null, string? updatedBy = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Set multiple settings at once
    /// </summary>
    Task SetSettingsAsync(Dictionary<string, string> settings, string? category = null, string? updatedBy = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a setting (soft delete - sets IsActive to false)
    /// </summary>
    Task DeleteSettingAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a setting exists and is active
    /// </summary>
    Task<bool> SettingExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refresh cached settings
    /// </summary>
    Task RefreshCacheAsync(CancellationToken cancellationToken = default);
}
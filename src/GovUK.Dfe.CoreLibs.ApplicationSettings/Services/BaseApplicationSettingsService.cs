using GovUK.Dfe.CoreLibs.ApplicationSettings.Configuration;
using GovUK.Dfe.CoreLibs.ApplicationSettings.Entities;
using GovUK.Dfe.CoreLibs.ApplicationSettings.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace GovUK.Dfe.CoreLibs.ApplicationSettings.Services;

public abstract class BaseApplicationSettingsService : IApplicationSettingsService
{
    protected readonly IMemoryCache _cache;
    protected readonly ApplicationSettingsOptions _options;
    protected readonly ILogger _logger;

    protected const string CacheKeyPrefix = "AppSettings_";
    protected const string AllSettingsCacheKey = "AppSettings_All";

    protected BaseApplicationSettingsService(
        IMemoryCache cache,
        IOptions<ApplicationSettingsOptions> options,
        ILogger logger)
    {
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    protected abstract DbSet<ApplicationSetting> GetApplicationSettingsDbSet();
    protected abstract Task SaveChangesAsync(CancellationToken cancellationToken);

    public async Task<string?> GetSettingAsync(string key, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);

        var cacheKey = BuildCacheKey(key);
        if (TryGetCachedValue(cacheKey, out string? cachedValue))
        {
            return cachedValue;
        }

        var setting = await GetApplicationSettingsDbSet()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == key && s.IsActive, cancellationToken);

        var value = setting?.Value;
        CacheValueIfEnabled(cacheKey, value);

        return value;
    }

    public async Task<T?> GetSettingAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        var value = await GetSettingAsync(key, cancellationToken);
        if (value == null) return null;

        return DeserializeValue<T>(value, key);
    }

    public async Task<string> GetSettingAsync(string key, string defaultValue, CancellationToken cancellationToken = default)
    {
        var value = await GetSettingAsync(key, cancellationToken);
        return value ?? defaultValue;
    }

    public async Task<Dictionary<string, string>> GetSettingsByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        ValidateCategory(category);

        var cacheKey = BuildCategoryCacheKey(category);
        if (TryGetCachedValue(cacheKey, out Dictionary<string, string>? cachedSettings))
        {
            return cachedSettings!;
        }

        var settings = await GetApplicationSettingsDbSet()
            .AsNoTracking()
            .Where(s => s.Category == category && s.IsActive)
            .ToDictionaryAsync(s => s.Key, s => s.Value, cancellationToken);

        CacheValueIfEnabled(cacheKey, settings);
        return settings;
    }

    public async Task<Dictionary<string, string>> GetAllSettingsAsync(CancellationToken cancellationToken = default)
    {
        if (TryGetCachedValue(AllSettingsCacheKey, out Dictionary<string, string>? cachedAllSettings))
        {
            return cachedAllSettings!;
        }

        var settings = await GetApplicationSettingsDbSet()
            .AsNoTracking()
            .Where(s => s.IsActive)
            .ToDictionaryAsync(s => s.Key, s => s.Value, cancellationToken);

        CacheValueIfEnabled(AllSettingsCacheKey, settings);
        return settings;
    }

    public async Task SetSettingAsync(string key, string value, string? description = null, string? category = null, string? updatedBy = null, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        ValidateValue(value);

        var setting = await GetApplicationSettingsDbSet()
            .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

        var effectiveCategory = category ?? _options.DefaultCategory;

        if (setting == null)
        {
            setting = CreateNewSetting(key, value, description, effectiveCategory, updatedBy);
            GetApplicationSettingsDbSet().Add(setting);
        }
        else
        {
            UpdateExistingSetting(setting, value, description, effectiveCategory, updatedBy);
        }

        await SaveChangesAsync(cancellationToken);
        InvalidateRelatedCache(key, effectiveCategory);
        LogSettingUpdate(key, updatedBy);
    }

    public async Task SetSettingsAsync(Dictionary<string, string> settings, string? category = null, string? updatedBy = null, CancellationToken cancellationToken = default)
    {
        if (settings == null || settings.Count == 0) return;

        foreach (var kvp in settings)
        {
            await SetSettingAsync(kvp.Key, kvp.Value, null, category, updatedBy, cancellationToken);
        }
    }

    public async Task DeleteSettingAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key)) return;

        var setting = await GetApplicationSettingsDbSet()
            .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

        if (setting != null)
        {
            setting.IsActive = false;
            setting.UpdatedAt = DateTime.UtcNow;
            await SaveChangesAsync(cancellationToken);

            InvalidateRelatedCache(key, setting.Category);
            _logger.LogInformation("Setting {Key} deleted", key);
        }
    }

    public async Task<bool> SettingExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;

        return await GetApplicationSettingsDbSet()
            .AnyAsync(s => s.Key == key && s.IsActive, cancellationToken);
    }

    public async Task RefreshCacheAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.EnableCaching) return;

        var allSettings = await GetApplicationSettingsDbSet()
            .Where(s => s.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var setting in allSettings)
        {
            var cacheKey = BuildCacheKey(setting.Key);
            CacheValueIfEnabled(cacheKey, setting.Value);
        }

        _logger.LogInformation("Application settings cache refreshed with {Count} settings", allSettings.Count);
    }

    #region Helper Methods

    protected static string BuildCacheKey(string key) => $"{CacheKeyPrefix}{key}";
    protected static string BuildCategoryCacheKey(string category) => $"{CacheKeyPrefix}Category_{category}";

    protected bool TryGetCachedValue<T>(string cacheKey, out T? cachedValue) where T : class
    {
        cachedValue = default;
        return _options.EnableCaching && _cache.TryGetValue(cacheKey, out cachedValue);
    }

    protected void CacheValueIfEnabled<T>(string cacheKey, T? value) where T : class
    {
        if (_options.EnableCaching && value != null)
        {
            var cacheOptions = CreateCacheOptions();
            _cache.Set(cacheKey, value, cacheOptions);
        }
    }

    protected MemoryCacheEntryOptions CreateCacheOptions()
    {
        return new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.CacheExpirationMinutes)
        };
    }

    protected void InvalidateRelatedCache(string key, string category)
    {
        if (_options.EnableCaching)
        {
            _cache.Remove(BuildCacheKey(key));
            _cache.Remove(BuildCategoryCacheKey(category));
            _cache.Remove(AllSettingsCacheKey);
        }
    }

    protected T? DeserializeValue<T>(string value, string key) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(value);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize setting {Key} to type {Type}", key, typeof(T).Name);
            return null;
        }
    }

    protected static ApplicationSetting CreateNewSetting(string key, string value, string? description, string category, string? updatedBy)
    {
        return new ApplicationSetting
        {
            Key = key,
            Value = value,
            Description = description,
            Category = category,
            CreatedBy = updatedBy,
            UpdatedBy = updatedBy
        };
    }

    protected static void UpdateExistingSetting(ApplicationSetting setting, string value, string? description, string category, string? updatedBy)
    {
        setting.Value = value;
        setting.Description = description ?? setting.Description;
        setting.Category = category ?? setting.Category;
        setting.UpdatedAt = DateTime.UtcNow;
        setting.UpdatedBy = updatedBy;
        setting.IsActive = true;
    }

    protected void LogSettingUpdate(string key, string? updatedBy)
    {
        _logger.LogInformation("Setting {Key} updated by {UpdatedBy}", key, updatedBy ?? "System");
    }

    #endregion

    #region Validation Methods

    protected static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Setting key cannot be null or empty", nameof(key));
    }

    protected static void ValidateValue(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
    }

    protected static void ValidateCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category cannot be null or empty", nameof(category));
    }

    #endregion
}
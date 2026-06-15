namespace GovUK.Dfe.CoreLibs.ApplicationSettings.Configuration;

public class ApplicationSettingsOptions
{
    public const string ConfigurationSection = "ApplicationSettings";

    /// <summary>
    /// Database schema name for the ApplicationSettings table
    /// </summary>
    public string? Schema { get; set; } = null;

    /// <summary>
    /// Enable caching of settings in memory
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Cache expiration time in minutes
    /// </summary>
    public int CacheExpirationMinutes { get; set; } = 30;

    /// <summary>
    /// Default category for settings without specified category
    /// </summary>
    public string DefaultCategory { get; set; } = "General";
}
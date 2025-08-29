namespace DfE.CoreLibs.FileStorage.Settings;

/// <summary>
/// Settings required for the local file storage provider.
/// </summary>
public class LocalFileStorageOptions
{
    /// <summary>
    /// Base directory path where files will be stored. 
    /// If not specified, defaults to a "FileStorage" folder in the application's base directory.
    /// </summary>
    public string BaseDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Whether to create the base directory if it doesn't exist. Default is true.
    /// </summary>
    public bool CreateDirectoryIfNotExists { get; set; } = true;

    /// <summary>
    /// Whether to allow overwriting existing files during upload. Default is true.
    /// </summary>
    public bool AllowOverwrite { get; set; } = true;

    /// <summary>
    /// Maximum file size in bytes. Default is 100MB (100 * 1024 * 1024).
    /// Set to 0 to disable size checking.
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 100 * 1024 * 1024; // 100MB

    /// <summary>
    /// Array of allowed file extensions (without the dot). 
    /// If null or empty, all extensions are allowed.
    /// Example: ["jpg", "png", "pdf", "docx"]
    /// </summary>
    public string[] AllowedExtensions { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Friendly human-readable list of allowed file extensions.
    /// Example: ["jpg", "png", "pdf", "docx"]
    /// </summary>
    public string AllowedExtensionsFriendlyList { get; set; } = "\"jpg\", \"png\", \"pdf\", \"docx\"";

    /// <summary>
    /// Regular expression pattern for allowed file names. 
    /// If null or empty, no filename pattern validation is applied.
    /// Default pattern allows letters, numbers, underscores, and hyphens.
    /// Example: "^[a-zA-Z0-9_-]+$"
    /// </summary>
    public string? AllowedFileNamePattern { get; set; } = null;

    /// <summary>
    /// Friendly and human-readable list of allowed file name characters. 
    /// Example: a-zA-Z0-9_-
    /// </summary>
    public string AllowedFileNamePatternFriendlyList { get; set; } = "a-z A-Z 0-9 _ - no-space";
} 
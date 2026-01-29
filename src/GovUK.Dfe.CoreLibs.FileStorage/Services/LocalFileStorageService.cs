using GovUK.Dfe.CoreLibs.FileStorage.Interfaces;
using GovUK.Dfe.CoreLibs.FileStorage.Settings;
using GovUK.Dfe.CoreLibs.FileStorage.Exceptions;
using System.IO;
using System.Text.RegularExpressions;
using FileNotFoundException = GovUK.Dfe.CoreLibs.FileStorage.Exceptions.FileNotFoundException;

namespace GovUK.Dfe.CoreLibs.FileStorage.Services;

/// <summary>
/// Local file system based implementation of <see cref="IFileStorageService"/>.
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly string _baseDirectory;
    private readonly bool _createDirectoryIfNotExists;
    private readonly bool _allowOverwrite;
    private readonly long _maxFileSizeBytes;
    private readonly string[] _allowedExtensions;
    private readonly Regex? _allowedFileNamePattern;
    private readonly string _friendlyAllowedFileNamePattern;
    private readonly string _friendlyAllowedFileExtensionsPattern;

    /// <summary>
    /// Creates a new instance of the service using the provided configuration <paramref name="options"/>.
    /// </summary>
    /// <param name="options">Local file storage configuration.</param>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    /// <exception cref="FileStorageConfigurationException">Thrown when local configuration is invalid.</exception>
    public LocalFileStorageService(FileStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var localOptions = options.Local;
        _friendlyAllowedFileNamePattern = localOptions.AllowedFileNamePatternFriendlyList;
        _friendlyAllowedFileExtensionsPattern = localOptions.AllowedExtensionsFriendlyList;
        _createDirectoryIfNotExists = localOptions.CreateDirectoryIfNotExists;
        _allowOverwrite = localOptions.AllowOverwrite;
        _maxFileSizeBytes = localOptions.MaxFileSizeBytes;
        _allowedExtensions = localOptions.AllowedExtensions ?? Array.Empty<string>();

        // Initialize filename pattern if specified
        if (!string.IsNullOrWhiteSpace(localOptions.AllowedFileNamePattern))
        {
            try
            {
                _allowedFileNamePattern = new Regex(localOptions.AllowedFileNamePattern, RegexOptions.Compiled);
            }
            catch (ArgumentException ex)
            {
                throw new FileStorageConfigurationException($"Invalid filename pattern: {localOptions.AllowedFileNamePattern}", ex);
            }
        }

        // Determine base directory
        if (string.IsNullOrWhiteSpace(localOptions.BaseDirectory))
        {
            _baseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FileStorage");
        }
        else
        {
            _baseDirectory = Path.GetFullPath(localOptions.BaseDirectory);
        }

        // Create base directory if needed
        if (_createDirectoryIfNotExists && !Directory.Exists(_baseDirectory))
        {
            try
            {
                Directory.CreateDirectory(_baseDirectory);
            }
            catch (Exception ex)
            {
                throw new FileStorageConfigurationException($"Failed to create base directory '{_baseDirectory}'.", ex);
            }
        }

        // Validate base directory exists and is accessible
        if (!Directory.Exists(_baseDirectory))
        {
            throw new FileStorageConfigurationException($"Base directory '{_baseDirectory}' does not exist and cannot be created.");
        }
    }

    /// <summary>
    /// Internal constructor used for testing with custom settings.
    /// </summary>
    internal LocalFileStorageService(string baseDirectory,
        bool createDirectoryIfNotExists = true,
        bool allowOverwrite = true,
        long maxFileSizeBytes = 100 * 1024 * 1024,
        string[]? allowedExtensions = null,
        string? allowedFileNamePattern = null,
        string? friendlyAllowedFileNamePattern = "a-z A-Z 0-9 _ - no-space",
        string? friendlyAllowedFileExtensionsPattern = "\"jpg\", \"png\", \"pdf\", \"docx\"")
    {
        _baseDirectory = Path.GetFullPath(baseDirectory);
        _friendlyAllowedFileNamePattern = friendlyAllowedFileNamePattern ?? "a-z A-Z 0-9 _ - no-space";
        _friendlyAllowedFileExtensionsPattern = friendlyAllowedFileExtensionsPattern ?? "\"jpg\", \"png\", \"pdf\", \"docx\"";
        _createDirectoryIfNotExists = createDirectoryIfNotExists;
        _allowOverwrite = allowOverwrite;
        _maxFileSizeBytes = maxFileSizeBytes;
        _allowedExtensions = allowedExtensions ?? Array.Empty<string>();

        // Initialize filename pattern if specified
        if (!string.IsNullOrWhiteSpace(allowedFileNamePattern))
        {
            try
            {
                _allowedFileNamePattern = new Regex(allowedFileNamePattern, RegexOptions.Compiled);
            }
            catch (ArgumentException ex)
            {
                throw new FileStorageConfigurationException($"Invalid filename pattern: {allowedFileNamePattern}", ex);
            }
        }

        if (_createDirectoryIfNotExists && !Directory.Exists(_baseDirectory))
        {
            Directory.CreateDirectory(_baseDirectory);
        }
    }

    #region Public Methods - Default Options

    /// <inheritdoc />
    public Task UploadAsync(string path, Stream content, string? originalFileName = null, CancellationToken token = default)
    {
        return UploadInternalAsync(path, content, originalFileName, optionsOverride: null, token);
    }

    /// <inheritdoc />
    public Task<Stream> DownloadAsync(string path, CancellationToken token = default)
    {
        return DownloadInternalAsync(path, optionsOverride: null, token);
    }

    /// <inheritdoc />
    public Task DeleteAsync(string path, CancellationToken token = default)
    {
        return DeleteInternalAsync(path, optionsOverride: null, token);
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string path, CancellationToken token = default)
    {
        return ExistsInternalAsync(path, optionsOverride: null, token);
    }

    #endregion

    #region Public Methods - With Options Override (Multi-Tenant Support)

    /// <inheritdoc />
    public Task UploadAsync(string path, Stream content, string? originalFileName, LocalFileStorageOptions optionsOverride, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(optionsOverride);
        return UploadInternalAsync(path, content, originalFileName, optionsOverride, token);
    }

    /// <inheritdoc />
    public Task<Stream> DownloadAsync(string path, LocalFileStorageOptions optionsOverride, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(optionsOverride);
        return DownloadInternalAsync(path, optionsOverride, token);
    }

    /// <inheritdoc />
    public Task DeleteAsync(string path, LocalFileStorageOptions optionsOverride, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(optionsOverride);
        return DeleteInternalAsync(path, optionsOverride, token);
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string path, LocalFileStorageOptions optionsOverride, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(optionsOverride);
        return ExistsInternalAsync(path, optionsOverride, token);
    }

    #endregion

    #region Private Implementation Methods

    private async Task UploadInternalAsync(string path, Stream content, string? originalFileName, LocalFileStorageOptions? optionsOverride, CancellationToken token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(content);

        if (!content.CanRead)
            throw new ArgumentException("Stream must be readable.", nameof(content));

        // Determine effective settings
        var useOverride = optionsOverride != null;
        var baseDirectory = useOverride ? ResolveBaseDirectory(optionsOverride!) : _baseDirectory;
        var allowedExtensions = useOverride ? (optionsOverride!.AllowedExtensions ?? Array.Empty<string>()) : _allowedExtensions;
        var friendlyExtensionsPattern = useOverride ? optionsOverride!.AllowedExtensionsFriendlyList : _friendlyAllowedFileExtensionsPattern;
        var friendlyFileNamePattern = useOverride ? optionsOverride!.AllowedFileNamePatternFriendlyList : _friendlyAllowedFileNamePattern;
        var createDirIfNotExists = useOverride ? optionsOverride!.CreateDirectoryIfNotExists : _createDirectoryIfNotExists;
        var maxFileSize = useOverride ? optionsOverride!.MaxFileSizeBytes : _maxFileSizeBytes;
        var allowOverwrite = useOverride ? optionsOverride!.AllowOverwrite : _allowOverwrite;

        // Compile regex for override options (default options already have compiled regex)
        Regex? fileNamePattern = null;
        if (useOverride && !string.IsNullOrWhiteSpace(optionsOverride!.AllowedFileNamePattern))
        {
            try
            {
                fileNamePattern = new Regex(optionsOverride.AllowedFileNamePattern, RegexOptions.Compiled);
            }
            catch (ArgumentException ex)
            {
                throw new FileStorageConfigurationException($"Invalid filename pattern: {optionsOverride.AllowedFileNamePattern}", ex);
            }
        }
        else if (!useOverride)
        {
            fileNamePattern = _allowedFileNamePattern;
        }

        var fullPath = GetFullPath(path, baseDirectory);

        try
        {
            // Validate file extension if restrictions are configured
            if (allowedExtensions.Length > 0)
            {
                var fileExtension = Path.GetExtension(path);
                if (string.IsNullOrEmpty(fileExtension))
                {
                    throw new FileStorageException($"File extension is required. Allowed extensions: {friendlyExtensionsPattern}");
                }

                var extensionWithoutDot = fileExtension.TrimStart('.');
                if (!allowedExtensions.Contains(extensionWithoutDot, StringComparer.OrdinalIgnoreCase))
                {
                    throw new FileStorageException($"File extension '{extensionWithoutDot}' is not allowed. Allowed extensions: {friendlyExtensionsPattern}");
                }
            }

            // Validate filename pattern if configured
            if (fileNamePattern != null && originalFileName != null)
            {
                var fileName = Path.GetFileNameWithoutExtension(originalFileName);
                if (string.IsNullOrEmpty(fileName))
                {
                    throw new FileStorageException($"Filename is required when filename pattern validation is enabled. Pattern: {friendlyFileNamePattern}");
                }

                if (!fileNamePattern.IsMatch(fileName))
                {
                    throw new FileStorageException($"Filename '{fileName}' does not match the allowed pattern. Pattern: {friendlyFileNamePattern}");
                }
            }

            // Ensure directory exists
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                if (createDirIfNotExists)
                {
                    Directory.CreateDirectory(directory);
                }
                else
                {
                    throw new FileStorageException($"Directory '{directory}' does not exist and directory creation is disabled.");
                }
            }

            // Check file size if limit is set
            if (maxFileSize > 0 && content.Length > maxFileSize)
            {
                throw new FileStorageException($"File size {content.Length} bytes exceeds maximum allowed size of {maxFileSize} bytes.");
            }

            // Check if file exists and overwrite is not allowed
            if (File.Exists(fullPath) && !allowOverwrite)
            {
                throw new FileStorageException($"File '{path}' already exists and overwrite is not allowed.");
            }

            // Upload the file
            using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.Read);
            await content.CopyToAsync(fileStream, token);
        }
        catch (Exception ex) when (ex is not OperationCanceledException && ex is not FileStorageException && ex is not FileStorageConfigurationException)
        {
            throw new FileStorageException($"Failed to upload file at path '{path}'.", ex);
        }
    }

    private async Task<Stream> DownloadInternalAsync(string path, LocalFileStorageOptions? optionsOverride, CancellationToken token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var baseDirectory = optionsOverride != null ? ResolveBaseDirectory(optionsOverride) : _baseDirectory;
        var fullPath = GetFullPath(path, baseDirectory);

        try
        {
            // Check cancellation token
            token.ThrowIfCancellationRequested();

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"File not found at path '{path}'.");
            }

            return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
        catch (FileNotFoundException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new FileStorageException($"Failed to download file at path '{path}'.", ex);
        }
    }

    private async Task DeleteInternalAsync(string path, LocalFileStorageOptions? optionsOverride, CancellationToken token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var baseDirectory = optionsOverride != null ? ResolveBaseDirectory(optionsOverride) : _baseDirectory;
        var fullPath = GetFullPath(path, baseDirectory);

        try
        {
            // Check cancellation token
            token.ThrowIfCancellationRequested();

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new FileStorageException($"Failed to delete file at path '{path}'.", ex);
        }
    }

    private async Task<bool> ExistsInternalAsync(string path, LocalFileStorageOptions? optionsOverride, CancellationToken token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var baseDirectory = optionsOverride != null ? ResolveBaseDirectory(optionsOverride) : _baseDirectory;
        var fullPath = GetFullPath(path, baseDirectory);

        // Check cancellation token
        token.ThrowIfCancellationRequested();

        return File.Exists(fullPath);
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Resolves the base directory from options.
    /// </summary>
    private static string ResolveBaseDirectory(LocalFileStorageOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.BaseDirectory))
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FileStorage");
        }
        return Path.GetFullPath(options.BaseDirectory);
    }

    /// <summary>
    /// Gets the full file system path for the given relative path.
    /// </summary>
    private static string GetFullPath(string path, string baseDirectory)
    {
        // Normalize the path to prevent directory traversal attacks
        var normalizedPath = Path.GetFullPath(Path.Combine(baseDirectory, path));

        // Ensure the path is within the base directory
        if (!normalizedPath.StartsWith(baseDirectory, StringComparison.OrdinalIgnoreCase))
        {
            throw new FileStorageException($"Path '{path}' is outside the allowed base directory.");
        }

        return normalizedPath;
    }

    #endregion
}
using DfE.CoreLibs.FileStorage.Interfaces;
using DfE.CoreLibs.FileStorage.Settings;
using DfE.CoreLibs.FileStorage.Exceptions;
using System.IO;
using FileNotFoundException = DfE.CoreLibs.FileStorage.Exceptions.FileNotFoundException;

namespace DfE.CoreLibs.FileStorage.Services;

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
        _createDirectoryIfNotExists = localOptions.CreateDirectoryIfNotExists;
        _allowOverwrite = localOptions.AllowOverwrite;
        _maxFileSizeBytes = localOptions.MaxFileSizeBytes;
        _allowedExtensions = localOptions.AllowedExtensions ?? Array.Empty<string>();

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
    internal LocalFileStorageService(string baseDirectory, bool createDirectoryIfNotExists = true, bool allowOverwrite = true, long maxFileSizeBytes = 100 * 1024 * 1024, string[] allowedExtensions = null)
    {
        _baseDirectory = Path.GetFullPath(baseDirectory);
        _createDirectoryIfNotExists = createDirectoryIfNotExists;
        _allowOverwrite = allowOverwrite;
        _maxFileSizeBytes = maxFileSizeBytes;
        _allowedExtensions = allowedExtensions ?? Array.Empty<string>();

        if (_createDirectoryIfNotExists && !Directory.Exists(_baseDirectory))
        {
            Directory.CreateDirectory(_baseDirectory);
        }
    }

    /// <inheritdoc />
    public async Task UploadAsync(string path, Stream content, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(content);
        
        if (!content.CanRead)
            throw new ArgumentException("Stream must be readable.", nameof(content));

        var fullPath = GetFullPath(path);

        try
        {
            // Validate file extension if restrictions are configured
            if (_allowedExtensions.Length > 0)
            {
                var fileExtension = Path.GetExtension(path);
                if (string.IsNullOrEmpty(fileExtension))
                {
                    throw new FileStorageException($"File extension is required. Allowed extensions: {string.Join(", ", _allowedExtensions)}");
                }

                var extensionWithoutDot = fileExtension.TrimStart('.');
                if (!_allowedExtensions.Contains(extensionWithoutDot, StringComparer.OrdinalIgnoreCase))
                {
                    throw new FileStorageException($"File extension '{extensionWithoutDot}' is not allowed. Allowed extensions: {string.Join(", ", _allowedExtensions)}");
                }
            }

            // Ensure directory exists
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                if (_createDirectoryIfNotExists)
                {
                    Directory.CreateDirectory(directory);
                }
                else
                {
                    throw new FileStorageException($"Directory '{directory}' does not exist and directory creation is disabled.");
                }
            }

            // Check file size if limit is set
            if (_maxFileSizeBytes > 0 && content.Length > _maxFileSizeBytes)
            {
                throw new FileStorageException($"File size {content.Length} bytes exceeds maximum allowed size of {_maxFileSizeBytes} bytes.");
            }

            // Check if file exists and overwrite is not allowed
            if (File.Exists(fullPath) && !_allowOverwrite)
            {
                throw new FileStorageException($"File '{path}' already exists and overwrite is not allowed.");
            }

            // Upload the file
            using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.Read);
            await content.CopyToAsync(fileStream, token);
        }
        catch (Exception ex) when (ex is not OperationCanceledException && ex is not FileStorageException)
        {
            throw new FileStorageException($"Failed to upload file at path '{path}'.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<Stream> DownloadAsync(string path, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = GetFullPath(path);

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

    /// <inheritdoc />
    public async Task DeleteAsync(string path, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = GetFullPath(path);

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

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string path, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = GetFullPath(path);
        
        // Check cancellation token
        token.ThrowIfCancellationRequested();
        
        return File.Exists(fullPath);
    }

    /// <summary>
    /// Gets the full file system path for the given relative path.
    /// </summary>
    /// <param name="path">Relative path within the storage.</param>
    /// <returns>Full file system path.</returns>
    private string GetFullPath(string path)
    {
        // Normalize the path to prevent directory traversal attacks
        var normalizedPath = Path.GetFullPath(Path.Combine(_baseDirectory, path));
        
        // Ensure the path is within the base directory
        if (!normalizedPath.StartsWith(_baseDirectory, StringComparison.OrdinalIgnoreCase))
        {
            throw new FileStorageException($"Path '{path}' is outside the allowed base directory.");
        }

        return normalizedPath;
    }
} 
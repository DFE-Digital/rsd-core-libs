using GovUK.Dfe.CoreLibs.FileStorage.Interfaces;
using GovUK.Dfe.CoreLibs.FileStorage.Settings;
using GovUK.Dfe.CoreLibs.FileStorage.Exceptions;
using System.IO;

namespace GovUK.Dfe.CoreLibs.FileStorage.Services;

/// <summary>
/// Hybrid file storage implementation that combines local storage for file operations
/// with Azure storage for SAS token generation.
/// </summary>
/// <remarks>
/// This service uses local file system for Upload, Download, Delete, and Exists operations,
/// while delegating SAS token generation to Azure File Storage.
/// This is useful when you want the performance and simplicity of local storage
/// but need Azure-specific features like generating secure access tokens.
/// </remarks>
public class HybridFileStorageService : IFileStorageService, IAzureSpecificOperations
{
    private readonly LocalFileStorageService _localService;
    private readonly AzureFileStorageService _azureService;

    /// <summary>
    /// Creates a new instance of the hybrid service using the provided configuration <paramref name="options"/>.
    /// </summary>
    /// <param name="options">File storage configuration containing both local and Azure settings.</param>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    /// <exception cref="FileStorageConfigurationException">Thrown when configuration is invalid.</exception>
    public HybridFileStorageService(FileStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // Validate both local and Azure configurations are present
        if (string.IsNullOrWhiteSpace(options.Azure.ConnectionString))
        {
            throw new FileStorageConfigurationException("Azure connection string is required for hybrid mode.");
        }

        if (string.IsNullOrWhiteSpace(options.Azure.ShareName))
        {
            throw new FileStorageConfigurationException("Azure share name is required for hybrid mode.");
        }

        // Initialize both services
        _localService = new LocalFileStorageService(options);
        _azureService = new AzureFileStorageService(options);
    }

    /// <summary>
    /// Internal constructor used for testing with custom service instances.
    /// </summary>
    internal HybridFileStorageService(LocalFileStorageService localService, AzureFileStorageService azureService)
    {
        _localService = localService ?? throw new ArgumentNullException(nameof(localService));
        _azureService = azureService ?? throw new ArgumentNullException(nameof(azureService));
    }

    #region IFileStorageService Implementation - Delegated to Local Storage

    /// <inheritdoc />
    /// <remarks>This operation is handled by local file storage.</remarks>
    public async Task UploadAsync(string path, Stream content, string? originalFileName = null, CancellationToken token = default)
    {
        await _localService.UploadAsync(path, content, originalFileName, token);
    }

    /// <inheritdoc />
    /// <remarks>This operation is handled by local file storage.</remarks>
    public async Task<Stream> DownloadAsync(string path, CancellationToken token = default)
    {
        return await _localService.DownloadAsync(path, token);
    }

    /// <inheritdoc />
    /// <remarks>This operation is handled by local file storage.</remarks>
    public async Task DeleteAsync(string path, CancellationToken token = default)
    {
        await _localService.DeleteAsync(path, token);
    }

    /// <inheritdoc />
    /// <remarks>This operation is handled by local file storage.</remarks>
    public async Task<bool> ExistsAsync(string path, CancellationToken token = default)
    {
        return await _localService.ExistsAsync(path, token);
    }

    #endregion

    #region IAzureSpecificOperations Implementation - Delegated to Azure Storage

    /// <inheritdoc />
    /// <remarks>
    /// This operation is handled by Azure file storage.
    /// Note: The file must exist in Azure storage for SAS token generation to work.
    /// </remarks>
    public async Task<string> GenerateSasTokenAsync(string path, DateTimeOffset expiresOn, string permissions = "r", CancellationToken token = default)
    {
        return await _azureService.GenerateSasTokenAsync(path, expiresOn, permissions, token);
    }

    /// <inheritdoc />
    /// <remarks>
    /// This operation is handled by Azure file storage.
    /// Note: The file must exist in Azure storage for SAS token generation to work.
    /// </remarks>
    public async Task<string> GenerateSasTokenAsync(string path, TimeSpan duration, string permissions = "r", CancellationToken token = default)
    {
        return await _azureService.GenerateSasTokenAsync(path, duration, permissions, token);
    }

    #endregion
}


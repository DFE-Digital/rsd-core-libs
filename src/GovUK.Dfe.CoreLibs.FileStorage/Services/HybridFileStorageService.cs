using GovUK.Dfe.CoreLibs.FileStorage.Interfaces;
using GovUK.Dfe.CoreLibs.FileStorage.Settings;
using GovUK.Dfe.CoreLibs.FileStorage.Exceptions;
using System.IO;

namespace GovUK.Dfe.CoreLibs.FileStorage.Services;

/// <summary>
/// Hybrid file storage implementation that combines local storage for file operations
/// with Azure storage for SAS token generation.
/// </summary>
public class HybridFileStorageService : IFileStorageService, IAzureSpecificOperations
{
    private readonly LocalFileStorageService _localService;
    private readonly AzureFileStorageService _azureService;

    /// <summary>
    /// Creates a new instance of the hybrid service using the provided configuration <paramref name="options"/>.
    /// </summary>
    public HybridFileStorageService(FileStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.Azure.ConnectionString))
        {
            throw new FileStorageConfigurationException("Azure connection string is required for hybrid mode.");
        }

        if (string.IsNullOrWhiteSpace(options.Azure.ShareName))
        {
            throw new FileStorageConfigurationException("Azure share name is required for hybrid mode.");
        }

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

    #region IFileStorageService - Default Options

    /// <inheritdoc />
    public async Task UploadAsync(string path, Stream content, string? originalFileName = null, CancellationToken token = default)
    {
        await _localService.UploadAsync(path, content, originalFileName, token);
    }

    /// <inheritdoc />
    public async Task<Stream> DownloadAsync(string path, CancellationToken token = default)
    {
        return await _localService.DownloadAsync(path, token);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string path, CancellationToken token = default)
    {
        await _localService.DeleteAsync(path, token);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string path, CancellationToken token = default)
    {
        return await _localService.ExistsAsync(path, token);
    }

    #endregion

    #region IFileStorageService - With Options Override (Multi-Tenant Support)

    /// <inheritdoc />
    public async Task UploadAsync(string path, Stream content, string? originalFileName, LocalFileStorageOptions? optionsOverride, CancellationToken token = default)
    {
        await _localService.UploadAsync(path, content, originalFileName, optionsOverride, token);
    }

    /// <inheritdoc />
    public async Task<Stream> DownloadAsync(string path, LocalFileStorageOptions? optionsOverride, CancellationToken token = default)
    {
        return await _localService.DownloadAsync(path, optionsOverride, token);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string path, LocalFileStorageOptions? optionsOverride, CancellationToken token = default)
    {
        await _localService.DeleteAsync(path, optionsOverride, token);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string path, LocalFileStorageOptions? optionsOverride, CancellationToken token = default)
    {
        return await _localService.ExistsAsync(path, optionsOverride, token);
    }

    #endregion

    #region IAzureSpecificOperations

    /// <inheritdoc />
    public async Task<string> GenerateSasTokenAsync(string path, DateTimeOffset expiresOn, string permissions = "r", CancellationToken token = default)
    {
        return await _azureService.GenerateSasTokenAsync(path, expiresOn, permissions, token);
    }

    /// <inheritdoc />
    public async Task<string> GenerateSasTokenAsync(string path, TimeSpan duration, string permissions = "r", CancellationToken token = default)
    {
        return await _azureService.GenerateSasTokenAsync(path, duration, permissions, token);
    }

    #endregion
}
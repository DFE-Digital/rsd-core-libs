using GovUK.Dfe.CoreLibs.FileStorage.Interfaces;
using GovUK.Dfe.CoreLibs.FileStorage.Settings;
using GovUK.Dfe.CoreLibs.FileStorage.Clients;
using GovUK.Dfe.CoreLibs.FileStorage.Exceptions;
using System.IO;
using FileNotFoundException = GovUK.Dfe.CoreLibs.FileStorage.Exceptions.FileNotFoundException;

namespace GovUK.Dfe.CoreLibs.FileStorage.Services;

/// <summary>
/// Azure File Service based implementation of <see cref="IFileStorageService"/> and <see cref="IAzureSpecificOperations"/>.
/// </summary>
public class AzureFileStorageService : IFileStorageService, IAzureSpecificOperations
{
    private readonly IShareClientWrapper _clientWrapper;

    public AzureFileStorageService(FileStorageOptions options)
        : this(CreateClientWrapper(options))
    {
    }

    private static IShareClientWrapper CreateClientWrapper(FileStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.Azure.ConnectionString))
            throw new FileStorageConfigurationException("Azure connection string cannot be null or empty.");

        if (string.IsNullOrWhiteSpace(options.Azure.ShareName))
            throw new FileStorageConfigurationException("Azure share name cannot be null or empty.");

        return new AzureShareClientWrapper(options.Azure.ConnectionString, options.Azure.ShareName);
    }

    internal AzureFileStorageService(IShareClientWrapper clientWrapper)
    {
        _clientWrapper = clientWrapper ?? throw new ArgumentNullException(nameof(clientWrapper));
    }

    #region IFileStorageService - Default

    /// <inheritdoc />
    public async Task UploadAsync(string path, Stream content, string? originalFileName = null, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(content);

        if (!content.CanRead)
            throw new ArgumentException("Stream must be readable.", nameof(content));

        try
        {
            var fileClient = await _clientWrapper.GetFileClientAsync(path, token);
            await fileClient.CreateAsync(content.Length, token);
            await fileClient.UploadAsync(content, token);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new FileStorageException($"Failed to upload file at path '{path}'.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<Stream> DownloadAsync(string path, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        try
        {
            var fileClient = await _clientWrapper.GetFileClientAsync(path, token);

            if (!await fileClient.ExistsAsync(token))
            {
                throw new FileNotFoundException($"File not found at path '{path}'.");
            }

            return await fileClient.DownloadAsync(token);
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

        try
        {
            var fileClient = await _clientWrapper.GetFileClientAsync(path, token);
            await fileClient.DeleteIfExistsAsync(token);
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

        try
        {
            var fileClient = await _clientWrapper.GetFileClientAsync(path, token);
            return await fileClient.ExistsAsync(token);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new FileStorageException($"Failed to check existence of file at path '{path}'.", ex);
        }
    }

    #endregion

    #region IFileStorageService - With Options Override (Not Supported for Azure)

    /// <inheritdoc />
    /// <remarks>Azure storage does not support LocalFileStorageOptions override. The optionsOverride parameter is ignored.</remarks>
    public Task UploadAsync(string path, Stream content, string? originalFileName, LocalFileStorageOptions? optionsOverride, CancellationToken token = default)
    {
        // Azure doesn't use LocalFileStorageOptions, just call the standard method
        return UploadAsync(path, content, originalFileName, token);
    }

    /// <inheritdoc />
    /// <remarks>Azure storage does not support LocalFileStorageOptions override. The optionsOverride parameter is ignored.</remarks>
    public Task<Stream> DownloadAsync(string path, LocalFileStorageOptions? optionsOverride, CancellationToken token = default)
    {
        return DownloadAsync(path, token);
    }

    /// <inheritdoc />
    /// <remarks>Azure storage does not support LocalFileStorageOptions override. The optionsOverride parameter is ignored.</remarks>
    public Task DeleteAsync(string path, LocalFileStorageOptions? optionsOverride, CancellationToken token = default)
    {
        return DeleteAsync(path, token);
    }

    /// <inheritdoc />
    /// <remarks>Azure storage does not support LocalFileStorageOptions override. The optionsOverride parameter is ignored.</remarks>
    public Task<bool> ExistsAsync(string path, LocalFileStorageOptions? optionsOverride, CancellationToken token = default)
    {
        return ExistsAsync(path, token);
    }

    #endregion

    #region IAzureSpecificOperations

    /// <inheritdoc />
    public async Task<string> GenerateSasTokenAsync(string path, DateTimeOffset expiresOn, string permissions = "r", CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(permissions);

        if (expiresOn <= DateTimeOffset.UtcNow)
        {
            throw new ArgumentException("Expiration date must be in the future.", nameof(expiresOn));
        }

        try
        {
            var fileClient = await _clientWrapper.GetFileClientAsync(path, token);

            if (!await fileClient.ExistsAsync(token))
            {
                throw new FileNotFoundException($"File not found at path '{path}'. Cannot generate SAS token for non-existent file.");
            }

            return await fileClient.GenerateSasUriAsync(expiresOn, permissions, token);
        }
        catch (FileNotFoundException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new FileStorageException($"Failed to generate SAS token for file at path '{path}'.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<string> GenerateSasTokenAsync(string path, TimeSpan duration, string permissions = "r", CancellationToken token = default)
    {
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentException("Duration must be greater than zero.", nameof(duration));
        }

        var expiresOn = DateTimeOffset.UtcNow.Add(duration);
        return await GenerateSasTokenAsync(path, expiresOn, permissions, token);
    }

    #endregion
}
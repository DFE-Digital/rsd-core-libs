using DfE.CoreLibs.FileStorage.Interfaces;
using DfE.CoreLibs.FileStorage.Settings;
using DfE.CoreLibs.FileStorage.Clients;
using DfE.CoreLibs.FileStorage.Exceptions;
using System.IO;
using FileNotFoundException = DfE.CoreLibs.FileStorage.Exceptions.FileNotFoundException;

namespace DfE.CoreLibs.FileStorage.Services;

/// <summary>
/// Azure File Service based implementation of <see cref="IFileStorageService"/>.
/// </summary>
public class AzureFileStorageService : IFileStorageService
{
    private readonly IShareClientWrapper _clientWrapper;

    /// <summary>
    /// Creates a new instance of the service using the provided configuration <paramref name="options"/>.
    /// </summary>
    /// <param name="options">Azure file storage configuration.</param>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    /// <exception cref="FileStorageConfigurationException">Thrown when Azure configuration is invalid.</exception>
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

    /// <summary>
    /// Internal constructor used for testing with a custom client wrapper.
    /// </summary>
    internal AzureFileStorageService(IShareClientWrapper clientWrapper)
    {
        _clientWrapper = clientWrapper ?? throw new ArgumentNullException(nameof(clientWrapper));
    }

    /// <inheritdoc />
    public async Task UploadAsync(string path, Stream content, string? originalFileName = null,  CancellationToken token = default)
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
            
            // Check if file exists before attempting download
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
}

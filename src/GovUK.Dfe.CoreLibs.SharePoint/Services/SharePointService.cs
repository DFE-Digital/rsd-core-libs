using GovUK.Dfe.CoreLibs.SharePoint.Clients;
using GovUK.Dfe.CoreLibs.SharePoint.Exceptions;
using GovUK.Dfe.CoreLibs.SharePoint.Interfaces;
using GovUK.Dfe.CoreLibs.SharePoint.Models;
using Microsoft.Extensions.Logging;

namespace GovUK.Dfe.CoreLibs.SharePoint.Services;

/// <summary>
/// SharePoint document library operations backed by Microsoft Graph.
/// </summary>
public sealed class SharePointService : ISharePointService
{
    private readonly IGraphClientWrapper _graphClient;
    private readonly ILogger<SharePointService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SharePointService"/> class.
    /// </summary>
    /// <param name="graphClient">Graph client wrapper.</param>
    /// <param name="logger">Logger.</param>
    internal SharePointService(IGraphClientWrapper graphClient, ILogger<SharePointService> logger)
    {
        _graphClient = graphClient ?? throw new ArgumentNullException(nameof(graphClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task CreateFolderAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeLibraryPath(folderPath);

        try
        {
            _logger.LogDebug("Creating SharePoint folder '{FolderPath}'.", normalized);
            await _graphClient.CreateFolderAsync(normalized, cancellationToken).ConfigureAwait(false);
        }
        catch (SharePointException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new SharePointException($"Failed to create folder '{normalized}'.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SharePointFileInfo>> ListFilesAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeLibraryPath(folderPath);

        try
        {
            _logger.LogDebug("Listing files in SharePoint folder '{FolderPath}'.", normalized);
            return await _graphClient.ListFilesAsync(normalized, cancellationToken).ConfigureAwait(false);
        }
        catch (SharePointException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new SharePointException($"Failed to list files in folder '{normalized}'.", ex);
        }
    }

    /// <inheritdoc />
    public async Task UploadFileAsync(string folderPath, string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(content);

        if (fileName.Contains('/') || fileName.Contains('\\'))
            throw new ArgumentException("File name must not contain path separators.", nameof(fileName));

        var normalized = NormalizeLibraryPath(folderPath);

        try
        {
            _logger.LogDebug("Uploading file '{FileName}' to SharePoint folder '{FolderPath}'.", fileName, normalized);
            await _graphClient.UploadFileAsync(normalized, fileName, content, cancellationToken).ConfigureAwait(false);
        }
        catch (SharePointException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new SharePointException($"Failed to upload file '{fileName}' to folder '{normalized}'.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<Stream> DownloadFileAsync(string folderPath, string fileName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        if (fileName.Contains('/') || fileName.Contains('\\'))
            throw new ArgumentException("File name must not contain path separators.", nameof(fileName));

        var normalized = NormalizeLibraryPath(folderPath);

        try
        {
            _logger.LogDebug("Downloading file '{FileName}' from SharePoint folder '{FolderPath}'.", fileName, normalized);
            return await _graphClient.DownloadFileAsync(normalized, fileName, cancellationToken).ConfigureAwait(false);
        }
        catch (SharePointException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new SharePointException($"Failed to download file '{fileName}' from folder '{normalized}'.", ex);
        }
    }

    /// <inheritdoc />
    public async Task DeleteFileAsync(string folderPath, string fileName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        if (fileName.Contains('/') || fileName.Contains('\\'))
            throw new ArgumentException("File name must not contain path separators.", nameof(fileName));

        var normalized = NormalizeLibraryPath(folderPath);

        try
        {
            _logger.LogDebug("Deleting file '{FileName}' from SharePoint folder '{FolderPath}'.", fileName, normalized);
            await _graphClient.DeleteFileAsync(normalized, fileName, cancellationToken).ConfigureAwait(false);
        }
        catch (SharePointException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new SharePointException($"Failed to delete file '{fileName}' from folder '{normalized}'.", ex);
        }
    }

    private static string NormalizeLibraryPath(string folderPath)
    {
        ArgumentNullException.ThrowIfNull(folderPath);

        var normalized = folderPath.Replace('\\', '/').Trim().Trim('/');
        if (string.IsNullOrEmpty(normalized))
            throw new ArgumentException(
                "Path must start with a document library name (e.g. Documents or Documents/folder).",
                nameof(folderPath));

        return normalized;
    }
}

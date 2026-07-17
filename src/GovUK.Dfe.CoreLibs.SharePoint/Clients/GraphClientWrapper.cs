using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;
using Azure.Core;
using Azure.Identity;
using GovUK.Dfe.CoreLibs.SharePoint.Exceptions;
using GovUK.Dfe.CoreLibs.SharePoint.Models;
using GovUK.Dfe.CoreLibs.SharePoint.Settings;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;

namespace GovUK.Dfe.CoreLibs.SharePoint.Clients;

/// <summary>
/// Microsoft Graph implementation of <see cref="IGraphClientWrapper"/>.
/// </summary>
internal sealed class GraphClientWrapper : IGraphClientWrapper
{
    private static readonly string[] GraphScopes = ["https://graph.microsoft.com/.default"];

    private readonly GraphServiceClient _graphClient;
    private readonly SharePointOptions _options;
    private readonly ConcurrentDictionary<string, string> _driveIdsByLibraryName =
        new(StringComparer.OrdinalIgnoreCase);

    public GraphClientWrapper(SharePointOptions options)
        : this(options, CreateGraphClient(options))
    {
    }

    internal GraphClientWrapper(SharePointOptions options, GraphServiceClient graphClient)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _graphClient = graphClient ?? throw new ArgumentNullException(nameof(graphClient));
    }

    public async Task CreateFolderAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        var (libraryName, relativePath) = SplitLibraryAndPath(folderPath);
        if (string.IsNullOrEmpty(relativePath))
            throw new SharePointException(
                "Folder path must include a folder within the document library.");

        var driveId = await ResolveDriveIdAsync(libraryName, cancellationToken).ConfigureAwait(false);
        var segments = SplitPath(relativePath);
        var currentPath = string.Empty;

        foreach (var segment in segments)
        {
            currentPath = string.IsNullOrEmpty(currentPath) ? segment : $"{currentPath}/{segment}";

            if (await FolderExistsAsync(driveId, currentPath, cancellationToken).ConfigureAwait(false))
                continue;

            var parentPath = GetParentPath(currentPath);
            var driveItem = new DriveItem
            {
                Name = segment,
                Folder = new Folder(),
                AdditionalData = new Dictionary<string, object>
                {
                    ["@microsoft.graph.conflictBehavior"] = "fail"
                }
            };

            try
            {
                if (string.IsNullOrEmpty(parentPath))
                {
                    await _graphClient.Drives[driveId].Items["root"].Children
                        .PostAsync(driveItem, cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    await _graphClient.Drives[driveId].Root
                        .ItemWithPath(parentPath)
                        .Children
                        .PostAsync(driveItem, cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            catch (ODataError ex) when (IsConflict(ex))
            {
                // Folder was created concurrently; continue.
            }
            catch (ODataError ex)
            {
                throw MapODataError(ex, $"Failed to create folder '{libraryName}/{currentPath}'.");
            }
        }
    }

    public async Task<IReadOnlyList<SharePointFileInfo>> ListFilesAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        var (libraryName, relativePath) = SplitLibraryAndPath(folderPath);
        var driveId = await ResolveDriveIdAsync(libraryName, cancellationToken).ConfigureAwait(false);
        var displayPath = string.IsNullOrEmpty(relativePath) ? libraryName : $"{libraryName}/{relativePath}";

        try
        {
            DriveItemCollectionResponse? response;

            if (string.IsNullOrEmpty(relativePath))
            {
                response = await _graphClient.Drives[driveId].Items["root"].Children
                    .GetAsync(cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                response = await _graphClient.Drives[driveId].Root
                    .ItemWithPath(relativePath)
                    .Children
                    .GetAsync(cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }

            var files = new List<SharePointFileInfo>();

            await foreach (var item in EnumeratePagesAsync(response, driveId, relativePath, cancellationToken).ConfigureAwait(false))
            {
                if (item.File is null || string.IsNullOrEmpty(item.Name))
                    continue;

                files.Add(new SharePointFileInfo
                {
                    Id = item.Id ?? string.Empty,
                    Name = item.Name,
                    Size = item.Size ?? 0,
                    LastModified = item.LastModifiedDateTime,
                    WebUrl = item.WebUrl
                });
            }

            return files;
        }
        catch (ODataError ex) when (IsNotFound(ex))
        {
            throw new SharePointNotFoundException($"Folder '{displayPath}' was not found.", ex);
        }
        catch (ODataError ex)
        {
            throw MapODataError(ex, $"Failed to list files in folder '{displayPath}'.");
        }
    }

    public async Task UploadFileAsync(string folderPath, string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(content);

        var (libraryName, relativePath) = SplitLibraryAndPath(folderPath);
        var driveId = await ResolveDriveIdAsync(libraryName, cancellationToken).ConfigureAwait(false);
        var itemPath = string.IsNullOrEmpty(relativePath) ? fileName : $"{relativePath}/{fileName}";
        var displayFolder = string.IsNullOrEmpty(relativePath) ? libraryName : $"{libraryName}/{relativePath}";

        try
        {
            await _graphClient.Drives[driveId].Root
                .ItemWithPath(itemPath)
                .Content
                .PutAsync(content, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (ODataError ex) when (IsNotFound(ex))
        {
            throw new SharePointNotFoundException($"Folder '{displayFolder}' was not found.", ex);
        }
        catch (ODataError ex)
        {
            throw MapODataError(ex, $"Failed to upload file '{libraryName}/{itemPath}'.");
        }
    }

    public async Task<Stream> DownloadFileAsync(string folderPath, string fileName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var (libraryName, relativePath) = SplitLibraryAndPath(folderPath);
        var driveId = await ResolveDriveIdAsync(libraryName, cancellationToken).ConfigureAwait(false);
        var itemPath = string.IsNullOrEmpty(relativePath) ? fileName : $"{relativePath}/{fileName}";

        try
        {
            var stream = await _graphClient.Drives[driveId].Root
                .ItemWithPath(itemPath)
                .Content
                .GetAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (stream is null)
                throw new SharePointNotFoundException($"File '{libraryName}/{itemPath}' was not found.");

            return stream;
        }
        catch (ODataError ex) when (IsNotFound(ex))
        {
            throw new SharePointNotFoundException($"File '{libraryName}/{itemPath}' was not found.", ex);
        }
        catch (ODataError ex)
        {
            throw MapODataError(ex, $"Failed to download file '{libraryName}/{itemPath}'.");
        }
    }

    public async Task DeleteFileAsync(string folderPath, string fileName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var (libraryName, relativePath) = SplitLibraryAndPath(folderPath);
        var driveId = await ResolveDriveIdAsync(libraryName, cancellationToken).ConfigureAwait(false);
        var itemPath = string.IsNullOrEmpty(relativePath) ? fileName : $"{relativePath}/{fileName}";

        try
        {
            await _graphClient.Drives[driveId].Root
                .ItemWithPath(itemPath)
                .DeleteAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (ODataError ex) when (IsNotFound(ex))
        {
            throw new SharePointNotFoundException($"File '{libraryName}/{itemPath}' was not found.", ex);
        }
        catch (ODataError ex)
        {
            throw MapODataError(ex, $"Failed to delete file '{libraryName}/{itemPath}'.");
        }
    }
    
    private async Task<string> ResolveDriveIdAsync(string libraryName, CancellationToken cancellationToken)
    {
        if (_driveIdsByLibraryName.TryGetValue(libraryName, out var cached))
            return cached;

        var siteId = await ResolveSiteIdAsync(cancellationToken).ConfigureAwait(false);

        DriveCollectionResponse? drives;
        try
        {
            drives = await _graphClient.Sites[siteId].Drives
                .GetAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (ODataError ex)
        {
            throw MapODataError(ex, "Failed to list document libraries for the configured site.");
        }

        var drive = drives?.Value?.FirstOrDefault(d =>
            string.Equals(d.Name, libraryName, StringComparison.OrdinalIgnoreCase));

        if (drive?.Id is null)
            throw new SharePointNotFoundException(
                $"Document library '{libraryName}' was not found on the configured site.");

        _driveIdsByLibraryName[libraryName] = drive.Id;
        return drive.Id;
    }

    private async Task<string> ResolveSiteIdAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_options.SiteId))
            return _options.SiteId;

        var siteKey = $"{_options.SiteHostname}:{_options.SitePath}";

        try
        {
            var site = await _graphClient.Sites[siteKey]
                .GetAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(site?.Id))
                throw new SharePointConfigurationException($"SharePoint site '{siteKey}' could not be resolved.");

            return site.Id;
        }
        catch (ODataError ex) when (IsNotFound(ex))
        {
            throw new SharePointNotFoundException($"SharePoint site '{siteKey}' was not found.", ex);
        }
        catch (ODataError ex)
        {
            throw MapODataError(ex, $"Failed to resolve SharePoint site '{siteKey}'.");
        }
    }

    private async Task<bool> FolderExistsAsync(string driveId, string folderPath, CancellationToken cancellationToken)
    {
        try
        {
            var item = await _graphClient.Drives[driveId].Root
                .ItemWithPath(folderPath)
                .GetAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return item?.Folder is not null;
        }
        catch (ODataError ex) when (IsNotFound(ex))
        {
            return false;
        }
        catch (ODataError ex)
        {
            throw MapODataError(ex, $"Failed to check whether folder '{folderPath}' exists.");
        }
    }

    private async IAsyncEnumerable<DriveItem> EnumeratePagesAsync(
        DriveItemCollectionResponse? firstPage,
        string driveId,
        string folderPath,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var page = firstPage;

        while (page is not null)
        {
            if (page.Value is not null)
            {
                foreach (var item in page.Value)
                    yield return item;
            }

            if (string.IsNullOrEmpty(page.OdataNextLink))
                yield break;

            try
            {
                if (string.IsNullOrEmpty(folderPath))
                {
                    page = await _graphClient.Drives[driveId].Items["root"].Children
                        .WithUrl(page.OdataNextLink)
                        .GetAsync(cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    page = await _graphClient.Drives[driveId].Root
                        .ItemWithPath(folderPath)
                        .Children
                        .WithUrl(page.OdataNextLink)
                        .GetAsync(cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            catch (ODataError ex)
            {
                throw MapODataError(ex, $"Failed to list files in folder '{folderPath}'.");
            }
        }
    }

    internal static GraphServiceClient CreateGraphClient(SharePointOptions options)
    {
        var credential = CreateCredential(options);
        return new GraphServiceClient(credential, GraphScopes);
    }

    internal static TokenCredential CreateCredential(SharePointOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.CertificatePath))
        {
            var certificate = string.IsNullOrEmpty(options.CertificatePassword)
                ? new X509Certificate2(options.CertificatePath)
                : new X509Certificate2(options.CertificatePath, options.CertificatePassword);

            return new ClientCertificateCredential(options.TenantId, options.ClientId, certificate);
        }

        return new ClientSecretCredential(options.TenantId, options.ClientId, options.ClientSecret);
    }

    private static (string LibraryName, string RelativePath) SplitLibraryAndPath(string folderPath)
    {
        var segments = SplitPath(folderPath);

        if (segments.Count == 0)
            throw new SharePointException(
                "Path must start with a document library name.");

        var libraryName = segments[0];
        var relativePath = segments.Count == 1
            ? string.Empty
            : string.Join('/', segments.Skip(1));

        return (libraryName, relativePath);
    }

    private static List<string> SplitPath(string path)
    {
        return NormalizePath(path)
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        return path.Replace('\\', '/').Trim().Trim('/');
    }

    private static string GetParentPath(string path)
    {
        var lastSlash = path.LastIndexOf('/');
        return lastSlash < 0 ? string.Empty : path[..lastSlash];
    }

    private static bool IsNotFound(ODataError error) =>
        string.Equals(error.Error?.Code, "itemNotFound", StringComparison.OrdinalIgnoreCase)
        || string.Equals(error.ResponseStatusCode.ToString(), "404", StringComparison.Ordinal)
        || error.ResponseStatusCode == 404;

    private static bool IsConflict(ODataError error) =>
        string.Equals(error.Error?.Code, "nameAlreadyExists", StringComparison.OrdinalIgnoreCase)
        || error.ResponseStatusCode == 409;

    private static SharePointException MapODataError(ODataError error, string message)
    {
        if (IsNotFound(error))
            return new SharePointNotFoundException(message, error);

        return new SharePointException($"{message} {error.Error?.Message}".Trim(), error);
    }
}

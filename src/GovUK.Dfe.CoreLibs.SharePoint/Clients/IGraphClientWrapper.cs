using GovUK.Dfe.CoreLibs.SharePoint.Models;

namespace GovUK.Dfe.CoreLibs.SharePoint.Clients;

/// <summary>
/// Thin wrapper around Microsoft Graph drive operations for testability.
/// </summary>
internal interface IGraphClientWrapper
{
    /// <summary>
    /// Creates a folder at the given path, creating missing parent folders as needed.
    /// </summary>
    Task CreateFolderAsync(string folderPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists files in the given folder (non-recursive).
    /// </summary>
    Task<IReadOnlyList<SharePointFileInfo>> ListFilesAsync(string folderPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a file to the given folder path, overwriting if it already exists.
    /// </summary>
    Task UploadFileAsync(string folderPath, string fileName, Stream content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a file from the given folder path.
    /// </summary>
    Task<Stream> DownloadFileAsync(string folderPath, string fileName, CancellationToken cancellationToken = default);
}

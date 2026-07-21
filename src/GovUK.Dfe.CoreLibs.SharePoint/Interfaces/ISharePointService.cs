using GovUK.Dfe.CoreLibs.SharePoint.Models;

namespace GovUK.Dfe.CoreLibs.SharePoint.Interfaces;

/// <summary>
/// Provides operations against SharePoint document libraries via Microsoft Graph.
/// Paths start with the document library name, then the folder path within that library
/// (e.g. <c>Documents/reports/2024</c>).
/// </summary>
public interface ISharePointService
{
    /// <summary>
    /// Creates a folder at the specified path, creating any missing parent folders.
    /// </summary>
    /// <param name="folderPath">
    /// Path including library name and folder (e.g. <c>Documents/reports/2024</c>).
    /// Must include at least one folder under the library.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CreateFolderAsync(string folderPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists files in the specified folder (non-recursive). Does not include subfolders.
    /// </summary>
    /// <param name="folderPath">
    /// Path including library name (e.g. <c>Documents</c> for the library root, or
    /// <c>Documents/reports</c> for a subfolder).
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>File metadata for files in the folder.</returns>
    Task<IReadOnlyList<SharePointFileInfo>> ListFilesAsync(string folderPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a file into the specified folder, overwriting an existing file with the same name.
    /// </summary>
    /// <param name="folderPath">
    /// Path including library name (e.g. <c>Documents</c> for the library root, or
    /// <c>Documents/reports</c> for a subfolder).
    /// </param>
    /// <param name="fileName">Name of the file to upload.</param>
    /// <param name="content">Stream containing the file contents.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UploadFileAsync(string folderPath, string fileName, Stream content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a file from the specified folder.
    /// </summary>
    /// <param name="folderPath">
    /// Path including library name (e.g. <c>Documents</c> for the library root, or
    /// <c>Documents/reports</c> for a subfolder).
    /// </param>
    /// <param name="fileName">Name of the file to download.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Stream containing the file contents.</returns>
    Task<Stream> DownloadFileAsync(string folderPath, string fileName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes a file from the specified folder.
    /// </summary>
    /// <param name="folderPath">
    /// Path including library name (e.g. <c>Documents</c> for the library root, or
    /// <c>Documents/reports</c> for a subfolder).
    /// </param>
    /// <param name="fileName">Name of the file to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteFileAsync(string folderPath, string fileName, CancellationToken cancellationToken = default);
}

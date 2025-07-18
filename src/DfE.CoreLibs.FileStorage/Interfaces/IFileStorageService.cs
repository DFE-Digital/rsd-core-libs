using System.IO;
namespace DfE.CoreLibs.FileStorage.Interfaces;

/// <summary>
/// Abstraction for storing and retrieving files.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Uploads the specified <paramref name="content"/> to the given <paramref name="path"/>.
    /// </summary>
    /// <param name="path">Relative path of the file within the storage provider.</param>
    /// <param name="content">Stream containing the data to upload.</param>
    /// <param name="token">Optional cancellation token.</param>
    Task UploadAsync(string path, Stream content, CancellationToken token = default);

    /// <summary>
    /// Retrieves a file from the specified <paramref name="path"/>.
    /// </summary>
    /// <param name="path">Relative path of the file to download.</param>
    /// <param name="token">Optional cancellation token.</param>
    /// <returns>A stream containing the file contents.</returns>
    Task<Stream> DownloadAsync(string path, CancellationToken token = default);

    /// <summary>
    /// Deletes the file at the specified <paramref name="path"/>, if it exists.
    /// </summary>
    /// <param name="path">Relative path of the file to delete.</param>
    /// <param name="token">Optional cancellation token.</param>
    Task DeleteAsync(string path, CancellationToken token = default);
}

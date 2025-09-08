using System.IO;
using GovUK.Dfe.CoreLibs.FileStorage.Exceptions;

namespace GovUK.Dfe.CoreLibs.FileStorage.Interfaces;

/// <summary>
/// Abstraction for storing and retrieving files from various storage providers.
/// This interface provides a consistent API for file operations regardless of the underlying storage implementation.
/// </summary>
/// <remarks>
/// Implementations of this interface should be thread-safe and handle storage-specific errors appropriately.
/// All methods support cancellation tokens for proper resource management.
/// </remarks>
public interface IFileStorageService
{
    /// <summary>
    /// Uploads the specified <paramref name="content"/> to the given <paramref name="path"/>.
    /// </summary>
    /// <param name="path">Relative path of the file within the storage provider. Must not be null or empty.</param>
    /// <param name="content">Stream containing the data to upload. Must be readable and not null.</param>
    /// <param name="originalFileName">Original Filename.</param>
    /// <param name="token">Optional cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous upload operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> or <paramref name="content"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is empty or <paramref name="content"/> is not readable.</exception>
    /// <exception cref="FileStorageException">Thrown when the upload operation fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    Task UploadAsync(string path, Stream content, string? originalFileName = null, CancellationToken token = default);

    /// <summary>
    /// Retrieves a file from the specified <paramref name="path"/>.
    /// </summary>
    /// <param name="path">Relative path of the file to download. Must not be null or empty.</param>
    /// <param name="token">Optional cancellation token to cancel the operation.</param>
    /// <returns>A stream containing the file contents. The caller is responsible for disposing the stream.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist at the specified path.</exception>
    /// <exception cref="FileStorageException">Thrown when the download operation fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    Task<Stream> DownloadAsync(string path, CancellationToken token = default);

    /// <summary>
    /// Deletes the file at the specified <paramref name="path"/>, if it exists.
    /// </summary>
    /// <param name="path">Relative path of the file to delete. Must not be null or empty.</param>
    /// <param name="token">Optional cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is empty.</exception>
    /// <exception cref="FileStorageException">Thrown when the delete operation fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    Task DeleteAsync(string path, CancellationToken token = default);

    /// <summary>
    /// Checks if a file exists at the specified <paramref name="path"/>.
    /// </summary>
    /// <param name="path">Relative path of the file to check. Must not be null or empty.</param>
    /// <param name="token">Optional cancellation token to cancel the operation.</param>
    /// <returns>True if the file exists, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is empty.</exception>
    /// <exception cref="FileStorageException">Thrown when the existence check fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    Task<bool> ExistsAsync(string path, CancellationToken token = default);
}

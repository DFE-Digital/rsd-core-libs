using GovUK.Dfe.CoreLibs.FileStorage.Settings;

namespace GovUK.Dfe.CoreLibs.FileStorage.Interfaces;

/// <summary>
/// Provides a provider-agnostic abstraction for file storage operations.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Uploads a file to the storage.
    /// </summary>
    /// <param name="path">Relative path including filename.</param>
    /// <param name="content">Stream containing the file contents.</param>
    /// <param name="originalFileName">Optional original filename for validation purposes.</param>
    /// <param name="token">Cancellation token.</param>
    Task UploadAsync(string path, Stream content, string? originalFileName = null, CancellationToken token = default);

    /// <summary>
    /// Uploads a file to the storage with optional runtime configuration override.
    /// </summary>
    /// <param name="path">Relative path including filename.</param>
    /// <param name="content">Stream containing the file contents.</param>
    /// <param name="originalFileName">Optional original filename for validation purposes.</param>
    /// <param name="optionsOverride">Optional local file storage options to override configured defaults. Use for multi-tenant scenarios.</param>
    /// <param name="token">Cancellation token.</param>
    Task UploadAsync(string path, Stream content, string? originalFileName, LocalFileStorageOptions? optionsOverride, CancellationToken token = default);

    /// <summary>
    /// Downloads a file from the storage.
    /// </summary>
    /// <param name="path">Relative path including filename.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>Stream containing the file contents.</returns>
    Task<Stream> DownloadAsync(string path, CancellationToken token = default);

    /// <summary>
    /// Downloads a file from the storage with optional runtime configuration override.
    /// </summary>
    /// <param name="path">Relative path including filename.</param>
    /// <param name="optionsOverride">Optional local file storage options to override configured defaults. Use for multi-tenant scenarios.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>Stream containing the file contents.</returns>
    Task<Stream> DownloadAsync(string path, LocalFileStorageOptions? optionsOverride, CancellationToken token = default);

    /// <summary>
    /// Deletes a file from the storage.
    /// </summary>
    /// <param name="path">Relative path including filename.</param>
    /// <param name="token">Cancellation token.</param>
    Task DeleteAsync(string path, CancellationToken token = default);

    /// <summary>
    /// Deletes a file from the storage with optional runtime configuration override.
    /// </summary>
    /// <param name="path">Relative path including filename.</param>
    /// <param name="optionsOverride">Optional local file storage options to override configured defaults. Use for multi-tenant scenarios.</param>
    /// <param name="token">Cancellation token.</param>
    Task DeleteAsync(string path, LocalFileStorageOptions? optionsOverride, CancellationToken token = default);

    /// <summary>
    /// Checks whether a file exists in the storage.
    /// </summary>
    /// <param name="path">Relative path including filename.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>True if the file exists; otherwise false.</returns>
    Task<bool> ExistsAsync(string path, CancellationToken token = default);

    /// <summary>
    /// Checks whether a file exists in the storage with optional runtime configuration override.
    /// </summary>
    /// <param name="path">Relative path including filename.</param>
    /// <param name="optionsOverride">Optional local file storage options to override configured defaults. Use for multi-tenant scenarios.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>True if the file exists; otherwise false.</returns>
    Task<bool> ExistsAsync(string path, LocalFileStorageOptions? optionsOverride, CancellationToken token = default);
}
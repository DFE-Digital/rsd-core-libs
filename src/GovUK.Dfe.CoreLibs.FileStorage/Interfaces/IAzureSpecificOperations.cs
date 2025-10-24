using GovUK.Dfe.CoreLibs.FileStorage.Exceptions;

namespace GovUK.Dfe.CoreLibs.FileStorage.Interfaces;

/// <summary>
/// Interface for Azure-specific file storage operations that are not available in other providers.
/// </summary>
public interface IAzureSpecificOperations
{
    /// <summary>
    /// Generates a Shared Access Signature (SAS) token for a specific file.
    /// </summary>
    /// <param name="path">Relative path of the file within the storage provider. Must not be null or empty.</param>
    /// <param name="expiresOn">The date and time when the SAS token expires.</param>
    /// <param name="permissions">Optional permissions for the SAS token. Defaults to read-only. Valid values: "r" (read), "w" (write), "d" (delete), or combinations like "rw".</param>
    /// <param name="token">Optional cancellation token to cancel the operation.</param>
    /// <returns>A SAS URI that can be used to access the file with the specified permissions.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is empty or <paramref name="expiresOn"/> is in the past.</exception>
    /// <exception cref="FileStorageException">Thrown when the SAS token generation fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    Task<string> GenerateSasTokenAsync(string path, DateTimeOffset expiresOn, string permissions = "r", CancellationToken token = default);

    /// <summary>
    /// Generates a Shared Access Signature (SAS) token for a specific file with a duration.
    /// </summary>
    /// <param name="path">Relative path of the file within the storage provider. Must not be null or empty.</param>
    /// <param name="duration">How long the SAS token should be valid for.</param>
    /// <param name="permissions">Optional permissions for the SAS token. Defaults to read-only. Valid values: "r" (read), "w" (write), "d" (delete), or combinations like "rw".</param>
    /// <param name="token">Optional cancellation token to cancel the operation.</param>
    /// <returns>A SAS URI that can be used to access the file with the specified permissions.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is empty or <paramref name="duration"/> is negative or zero.</exception>
    /// <exception cref="FileStorageException">Thrown when the SAS token generation fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    Task<string> GenerateSasTokenAsync(string path, TimeSpan duration, string permissions = "r", CancellationToken token = default);
}


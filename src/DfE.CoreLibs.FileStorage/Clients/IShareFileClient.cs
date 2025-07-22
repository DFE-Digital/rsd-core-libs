using System.IO;

namespace DfE.CoreLibs.FileStorage.Clients;

internal interface IShareFileClient
{
    Task CreateAsync(long size, CancellationToken token = default);
    Task UploadAsync(Stream content, CancellationToken token = default);
    Task<Stream> DownloadAsync(CancellationToken token = default);
    Task DeleteIfExistsAsync(CancellationToken token = default);
    Task<bool> ExistsAsync(CancellationToken token = default);
}

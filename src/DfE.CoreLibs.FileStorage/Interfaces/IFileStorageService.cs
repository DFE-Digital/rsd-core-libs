using System.IO;
namespace DfE.CoreLibs.FileStorage.Interfaces;

public interface IFileStorageService
{
    Task UploadAsync(string path, Stream content, CancellationToken token = default);
    Task<Stream> DownloadAsync(string path, CancellationToken token = default);
    Task DeleteAsync(string path, CancellationToken token = default);
}

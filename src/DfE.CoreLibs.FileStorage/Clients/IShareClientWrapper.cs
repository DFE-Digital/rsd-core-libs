namespace DfE.CoreLibs.FileStorage.Clients;

internal interface IShareClientWrapper
{
    Task<IShareFileClient> GetFileClientAsync(string path, CancellationToken token = default);
}

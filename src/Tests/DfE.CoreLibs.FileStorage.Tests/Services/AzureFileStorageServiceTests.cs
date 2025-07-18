using System.IO;
using System.Threading.Tasks;
using DfE.CoreLibs.FileStorage.Clients;
using DfE.CoreLibs.FileStorage.Services;
using NSubstitute;
using Xunit;

namespace DfE.CoreLibs.FileStorage.Tests.Services;

public class AzureFileStorageServiceTests
{
    [Fact]
    public async Task UploadAsync_CreatesAndUploadsFile()
    {
        var fileClient = Substitute.For<IShareFileClient>();
        var clientWrapper = Substitute.For<IShareClientWrapper>();
        clientWrapper.GetFileClientAsync("path.txt", Arg.Any<CancellationToken>()).Returns(fileClient);

        var service = new AzureFileStorageService(clientWrapper);
        using var stream = new MemoryStream(new byte[] {1,2,3});

        await service.UploadAsync("path.txt", stream);

        await fileClient.Received(1).CreateAsync(stream.Length, Arg.Any<CancellationToken>());
        await fileClient.Received(1).UploadAsync(stream, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DownloadAsync_ReturnsStreamFromClient()
    {
        var expectedStream = new MemoryStream(new byte[] {1,2,3});
        var fileClient = Substitute.For<IShareFileClient>();
        fileClient.DownloadAsync(Arg.Any<CancellationToken>()).Returns(expectedStream);
        var clientWrapper = Substitute.For<IShareClientWrapper>();
        clientWrapper.GetFileClientAsync("path.txt", Arg.Any<CancellationToken>()).Returns(fileClient);

        var service = new AzureFileStorageService(clientWrapper);

        var result = await service.DownloadAsync("path.txt");

        Assert.Equal(expectedStream, result);
    }

    [Fact]
    public async Task DeleteAsync_DeletesFile()
    {
        var fileClient = Substitute.For<IShareFileClient>();
        var clientWrapper = Substitute.For<IShareClientWrapper>();
        clientWrapper.GetFileClientAsync("path.txt", Arg.Any<CancellationToken>()).Returns(fileClient);

        var service = new AzureFileStorageService(clientWrapper);

        await service.DeleteAsync("path.txt");

        await fileClient.Received(1).DeleteIfExistsAsync(Arg.Any<CancellationToken>());
    }
}

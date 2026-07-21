using GovUK.Dfe.CoreLibs.SharePoint.Clients;
using GovUK.Dfe.CoreLibs.SharePoint.Exceptions;
using GovUK.Dfe.CoreLibs.SharePoint.Models;
using GovUK.Dfe.CoreLibs.SharePoint.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace GovUK.Dfe.CoreLibs.SharePoint.Tests.Services;

public class SharePointServiceTests
{
    private readonly IGraphClientWrapper _graphClient = Substitute.For<IGraphClientWrapper>();
    private readonly ILogger<SharePointService> _logger = Substitute.For<ILogger<SharePointService>>();
    private readonly SharePointService _sut;

    public SharePointServiceTests()
    {
        _sut = new SharePointService(_graphClient, _logger);
    }

    [Fact]
    public async Task CreateFolderAsync_NormalizesPath_AndDelegates()
    {
        await _sut.CreateFolderAsync(" Documents\\reports\\2024/ ");

        await _graphClient.Received(1).CreateFolderAsync("Documents/reports/2024", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateFolderAsync_LibraryRootOnly_Delegates()
    {
        await _sut.CreateFolderAsync("Documents");

        await _graphClient.Received(1).CreateFolderAsync("Documents", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateFolderAsync_NullPath_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.CreateFolderAsync(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("/")]
    public async Task CreateFolderAsync_EmptyPath_Throws(string path)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateFolderAsync(path));
    }

    [Fact]
    public async Task ListFilesAsync_ReturnsFilesFromClient()
    {
        var expected = new List<SharePointFileInfo>
        {
            new() { Id = "1", Name = "a.txt", Size = 10 }
        };
        _graphClient.ListFilesAsync("Documents/reports", Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.ListFilesAsync("Documents/reports");

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task ListFilesAsync_LibraryRoot_Delegates()
    {
        _graphClient.ListFilesAsync("Documents", Arg.Any<CancellationToken>())
            .Returns(Array.Empty<SharePointFileInfo>());

        await _sut.ListFilesAsync("Documents");

        await _graphClient.Received(1).ListFilesAsync("Documents", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListFilesAsync_NullPath_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.ListFilesAsync(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("/")]
    public async Task ListFilesAsync_EmptyPath_Throws(string path)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.ListFilesAsync(path));
    }

    [Fact]
    public async Task UploadFileAsync_DelegatesToClient()
    {
        await using var stream = new MemoryStream([1, 2, 3]);

        await _sut.UploadFileAsync("Documents/reports", "file.pdf", stream);

        await _graphClient.Received(1).UploadFileAsync("Documents/reports", "file.pdf", stream, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadFileAsync_NullContent_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.UploadFileAsync("Documents/reports", "file.pdf", null!));
    }

    [Fact]
    public async Task UploadFileAsync_FileNameWithPathSeparator_Throws()
    {
        await using var stream = new MemoryStream();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.UploadFileAsync("Documents/reports", "sub/file.pdf", stream));
    }

    [Fact]
    public async Task DownloadFileAsync_ReturnsStreamFromClient()
    {
        var expected = new MemoryStream([9, 8, 7]);
        _graphClient.DownloadFileAsync("Documents/reports", "file.pdf", Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.DownloadFileAsync("Documents/reports", "file.pdf");

        Assert.Same(expected, result);
    }
    
    [Fact]
    public async Task DeleteFileAsync_DelegatesToClient()
    {
        await _sut.DeleteFileAsync("Documents/reports", "file.pdf");
        await _graphClient.Received(1).DeleteFileAsync("Documents/reports", "file.pdf", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DownloadFileAsync_EmptyFileName_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.DownloadFileAsync("Documents/reports", " "));
    }
    
    [Fact]
    public async Task DeleteFileAsync_EmptyFileName_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.DeleteFileAsync("Documents/reports", " "));
    }
    
    [Fact]
    public async Task CreateFolderAsync_WrapsUnexpectedExceptions()
    {
        _graphClient.CreateFolderAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("boom"));

        var ex = await Assert.ThrowsAsync<SharePointException>(() =>
            _sut.CreateFolderAsync("Documents/reports"));

        Assert.Contains("Failed to create folder", ex.Message);
        Assert.IsType<InvalidOperationException>(ex.InnerException);
    }

    [Fact]
    public async Task ListFilesAsync_PropagatesSharePointException()
    {
        _graphClient.ListFilesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new SharePointNotFoundException("missing"));

        await Assert.ThrowsAsync<SharePointNotFoundException>(() =>
            _sut.ListFilesAsync("Documents/missing"));
    }
}

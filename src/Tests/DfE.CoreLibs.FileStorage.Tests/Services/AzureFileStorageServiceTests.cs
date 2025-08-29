using DfE.CoreLibs.FileStorage.Services;
using DfE.CoreLibs.FileStorage.Settings;
using DfE.CoreLibs.FileStorage.Clients;
using DfE.CoreLibs.FileStorage.Exceptions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
using FileNotFoundException = DfE.CoreLibs.FileStorage.Exceptions.FileNotFoundException;

namespace DfE.CoreLibs.FileStorage.Tests.Services;

public class AzureFileStorageServiceTests
{
    private readonly IShareClientWrapper _mockClientWrapper;
    private readonly IShareFileClient _mockFileClient;
    private readonly AzureFileStorageService _service;
    private readonly FileStorageOptions _validOptions;

    public AzureFileStorageServiceTests()
    {
        _mockClientWrapper = Substitute.For<IShareClientWrapper>();
        _mockFileClient = Substitute.For<IShareFileClient>();
        _service = new AzureFileStorageService(_mockClientWrapper);

        _validOptions = new FileStorageOptions
        {
            Provider = "Azure",
            Azure = new AzureFileStorageOptions
            {
                ConnectionString = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=test;EndpointSuffix=core.windows.net",
                ShareName = "testshare"
            }
        };
    }

    [Fact]
    public void Constructor_WithValidOptions_ShouldNotThrow()
    {
        // Act & Assert
        var service = new AzureFileStorageService(_validOptions);
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AzureFileStorageService((FileStorageOptions)null!));
    }

    [Fact]
    public void Constructor_WithNullConnectionString_ShouldThrowFileStorageConfigurationException()
    {
        // Arrange
        var options = new FileStorageOptions
        {
            Provider = "Azure",
            Azure = new AzureFileStorageOptions
            {
                ConnectionString = "",
                ShareName = "testshare"
            }
        };

        // Act & Assert
        var exception = Assert.Throws<FileStorageConfigurationException>(() => new AzureFileStorageService(options));
        Assert.Contains("Azure connection string cannot be null or empty", exception.Message);
    }

    [Fact]
    public void Constructor_WithNullShareName_ShouldThrowFileStorageConfigurationException()
    {
        // Arrange
        var options = new FileStorageOptions
        {
            Provider = "Azure",
            Azure = new AzureFileStorageOptions
            {
                ConnectionString = "test",
                ShareName = ""
            }
        };

        // Act & Assert
        var exception = Assert.Throws<FileStorageConfigurationException>(() => new AzureFileStorageService(options));
        Assert.Contains("Azure share name cannot be null or empty", exception.Message);
    }

    [Fact]
    public void Constructor_WithNullClientWrapper_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AzureFileStorageService((IShareClientWrapper)null!));
    }

    [Fact]
    public async Task UploadAsync_WithValidParameters_ShouldSucceed()
    {
        // Arrange
        var path = "test/file.txt";
        var content = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        _mockClientWrapper.GetFileClientAsync(path, Arg.Any<CancellationToken>()).Returns(_mockFileClient);

        // Act
        await _service.UploadAsync(path, content);

        // Assert
        await _mockClientWrapper.Received(1).GetFileClientAsync(path, Arg.Any<CancellationToken>());
        await _mockFileClient.Received(1).CreateAsync(content.Length, Arg.Any<CancellationToken>());
        await _mockFileClient.Received(1).UploadAsync(content, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadAsync_WithNullPath_ShouldThrowArgumentException()
    {
        // Arrange
        var content = new MemoryStream(new byte[] { 1, 2, 3 });

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.UploadAsync(null!, content));
    }

    [Fact]
    public async Task UploadAsync_WithEmptyPath_ShouldThrowArgumentException()
    {
        // Arrange
        var content = new MemoryStream(new byte[] { 1, 2, 3 });

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.UploadAsync("", content));
    }

    [Fact]
    public async Task UploadAsync_WithNullContent_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.UploadAsync("test.txt", null!));
    }

    [Fact]
    public async Task UploadAsync_WithNonReadableStream_ShouldThrowArgumentException()
    {
        // Arrange
        var nonReadableStream = Substitute.For<Stream>();
        nonReadableStream.CanRead.Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.UploadAsync("test.txt", nonReadableStream));
    }

    [Fact]
    public async Task UploadAsync_WhenClientWrapperThrows_ShouldThrowFileStorageException()
    {
        // Arrange
        var path = "test/file.txt";
        var content = new MemoryStream(new byte[] { 1, 2, 3 });
        _mockClientWrapper.GetFileClientAsync(path, Arg.Any<CancellationToken>()).ThrowsAsync(new Exception("Test exception"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FileStorageException>(() => _service.UploadAsync(path, content));
        Assert.Contains("Failed to upload file", exception.Message);
    }

    [Fact]
    public async Task DownloadAsync_WithValidParameters_ShouldSucceed()
    {
        // Arrange
        var path = "test/file.txt";
        var expectedStream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        _mockClientWrapper.GetFileClientAsync(path, Arg.Any<CancellationToken>()).Returns(_mockFileClient);
        _mockFileClient.ExistsAsync(Arg.Any<CancellationToken>()).Returns(true);
        _mockFileClient.DownloadAsync(Arg.Any<CancellationToken>()).Returns(expectedStream);

        // Act
        var result = await _service.DownloadAsync(path);

        // Assert
        Assert.Equal(expectedStream, result);
        await _mockClientWrapper.Received(1).GetFileClientAsync(path, Arg.Any<CancellationToken>());
        await _mockFileClient.Received(1).ExistsAsync(Arg.Any<CancellationToken>());
        await _mockFileClient.Received(1).DownloadAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DownloadAsync_WithNullPath_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.DownloadAsync(null!));
    }

    [Fact]
    public async Task DownloadAsync_WithEmptyPath_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.DownloadAsync(""));
    }

    [Fact]
    public async Task DownloadAsync_WhenFileDoesNotExist_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var path = "test/file.txt";
        _mockClientWrapper.GetFileClientAsync(path, Arg.Any<CancellationToken>()).Returns(_mockFileClient);
        _mockFileClient.ExistsAsync(Arg.Any<CancellationToken>()).Returns(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FileNotFoundException>(() => _service.DownloadAsync(path));
        Assert.Contains("File not found at path", exception.Message);
    }

    [Fact]
    public async Task DownloadAsync_WhenClientWrapperThrows_ShouldThrowFileStorageException()
    {
        // Arrange
        var path = "test/file.txt";
        _mockClientWrapper.GetFileClientAsync(path, Arg.Any<CancellationToken>()).ThrowsAsync(new Exception("Test exception"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FileStorageException>(() => _service.DownloadAsync(path));
        Assert.Contains("Failed to download file", exception.Message);
    }

    [Fact]
    public async Task DeleteAsync_WithValidParameters_ShouldSucceed()
    {
        // Arrange
        var path = "test/file.txt";
        _mockClientWrapper.GetFileClientAsync(path, Arg.Any<CancellationToken>()).Returns(_mockFileClient);

        // Act
        await _service.DeleteAsync(path);

        // Assert
        await _mockClientWrapper.Received(1).GetFileClientAsync(path, Arg.Any<CancellationToken>());
        await _mockFileClient.Received(1).DeleteIfExistsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WithNullPath_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.DeleteAsync(null!));
    }

    [Fact]
    public async Task DeleteAsync_WithEmptyPath_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.DeleteAsync(""));
    }

    [Fact]
    public async Task DeleteAsync_WhenClientWrapperThrows_ShouldThrowFileStorageException()
    {
        // Arrange
        var path = "test/file.txt";
        _mockClientWrapper.GetFileClientAsync(path, Arg.Any<CancellationToken>()).ThrowsAsync(new Exception("Test exception"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FileStorageException>(() => _service.DeleteAsync(path));
        Assert.Contains("Failed to delete file", exception.Message);
    }

    [Fact]
    public async Task ExistsAsync_WithValidParameters_ShouldReturnTrue()
    {
        // Arrange
        var path = "test/file.txt";
        _mockClientWrapper.GetFileClientAsync(path, Arg.Any<CancellationToken>()).Returns(_mockFileClient);
        _mockFileClient.ExistsAsync(Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _service.ExistsAsync(path);

        // Assert
        Assert.True(result);
        await _mockClientWrapper.Received(1).GetFileClientAsync(path, Arg.Any<CancellationToken>());
        await _mockFileClient.Received(1).ExistsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExistsAsync_WithValidParameters_ShouldReturnFalse()
    {
        // Arrange
        var path = "test/file.txt";
        _mockClientWrapper.GetFileClientAsync(path, Arg.Any<CancellationToken>()).Returns(_mockFileClient);
        _mockFileClient.ExistsAsync(Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _service.ExistsAsync(path);

        // Assert
        Assert.False(result);
        await _mockClientWrapper.Received(1).GetFileClientAsync(path, Arg.Any<CancellationToken>());
        await _mockFileClient.Received(1).ExistsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExistsAsync_WithNullPath_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.ExistsAsync(null!));
    }

    [Fact]
    public async Task ExistsAsync_WithEmptyPath_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.ExistsAsync(""));
    }

    [Fact]
    public async Task ExistsAsync_WhenClientWrapperThrows_ShouldThrowFileStorageException()
    {
        // Arrange
        var path = "test/file.txt";
        _mockClientWrapper.GetFileClientAsync(path, Arg.Any<CancellationToken>()).ThrowsAsync(new Exception("Test exception"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FileStorageException>(() => _service.ExistsAsync(path));
        Assert.Contains("Failed to check existence of file", exception.Message);
    }

    [Fact]
    public async Task AllMethods_WithCancellationToken_ShouldPassTokenToClient()
    {
        // Arrange
        var path = "test/file.txt";
        var content = new MemoryStream(new byte[] { 1, 2, 3 });
        var cancellationToken = new CancellationToken(true); // Cancelled token

        // Mock the client wrapper to throw OperationCanceledException when cancelled token is used
        _mockClientWrapper.GetFileClientAsync(path, cancellationToken).ThrowsAsync(new OperationCanceledException());
        _mockFileClient.ExistsAsync(cancellationToken).ThrowsAsync(new OperationCanceledException());
        _mockFileClient.DownloadAsync(cancellationToken).ThrowsAsync(new OperationCanceledException());

        // Act & Assert - All methods should pass the cancellation token and throw when cancelled
        await Assert.ThrowsAsync<OperationCanceledException>(() => _service.UploadAsync(path, content, originalFileName: null, cancellationToken));
        await Assert.ThrowsAsync<OperationCanceledException>(() => _service.DownloadAsync(path, cancellationToken));
        await Assert.ThrowsAsync<OperationCanceledException>(() => _service.DeleteAsync(path, cancellationToken));
        await Assert.ThrowsAsync<OperationCanceledException>(() => _service.ExistsAsync(path, cancellationToken));
    }
}
using GovUK.Dfe.CoreLibs.FileStorage.Services;
using GovUK.Dfe.CoreLibs.FileStorage.Settings;
using GovUK.Dfe.CoreLibs.FileStorage.Clients;
using GovUK.Dfe.CoreLibs.FileStorage.Exceptions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
using FileNotFoundException = GovUK.Dfe.CoreLibs.FileStorage.Exceptions.FileNotFoundException;

namespace GovUK.Dfe.CoreLibs.FileStorage.Tests.Services;

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

    #region SAS Token Generation Tests

    [Fact]
    public async Task GenerateSasTokenAsync_WithValidParametersAndExpiresOn_ShouldReturnSasUri()
    {
        // Arrange
        var path = "test/file.txt";
        var expiresOn = DateTimeOffset.UtcNow.AddHours(1);
        var expectedSasUri = "https://test.file.core.windows.net/testshare/test/file.txt?sv=2021-12-02&sr=f&sig=test";
        
        _mockClientWrapper.GetFileClientAsync(path, Arg.Any<CancellationToken>()).Returns(_mockFileClient);
        _mockFileClient.ExistsAsync(Arg.Any<CancellationToken>()).Returns(true);
        _mockFileClient.GenerateSasUriAsync(expiresOn, "r", Arg.Any<CancellationToken>()).Returns(expectedSasUri);

        // Act
        var result = await _service.GenerateSasTokenAsync(path, expiresOn, "r");

        // Assert
        Assert.Equal(expectedSasUri, result);
        await _mockClientWrapper.Received(1).GetFileClientAsync(path, Arg.Any<CancellationToken>());
        await _mockFileClient.Received(1).ExistsAsync(Arg.Any<CancellationToken>());
        await _mockFileClient.Received(1).GenerateSasUriAsync(expiresOn, "r", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateSasTokenAsync_WithValidParametersAndDuration_ShouldReturnSasUri()
    {
        // Arrange
        var path = "test/file.txt";
        var duration = TimeSpan.FromHours(2);
        var expectedSasUri = "https://test.file.core.windows.net/testshare/test/file.txt?sv=2021-12-02&sr=f&sig=test";
        
        _mockClientWrapper.GetFileClientAsync(path, Arg.Any<CancellationToken>()).Returns(_mockFileClient);
        _mockFileClient.ExistsAsync(Arg.Any<CancellationToken>()).Returns(true);
        _mockFileClient.GenerateSasUriAsync(Arg.Any<DateTimeOffset>(), "r", Arg.Any<CancellationToken>()).Returns(expectedSasUri);

        // Act
        var result = await _service.GenerateSasTokenAsync(path, duration, "r");

        // Assert
        Assert.Equal(expectedSasUri, result);
        await _mockClientWrapper.Received(1).GetFileClientAsync(path, Arg.Any<CancellationToken>());
        await _mockFileClient.Received(1).ExistsAsync(Arg.Any<CancellationToken>());
        await _mockFileClient.Received(1).GenerateSasUriAsync(Arg.Any<DateTimeOffset>(), "r", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateSasTokenAsync_WithWritePermissions_ShouldReturnSasUri()
    {
        // Arrange
        var path = "test/file.txt";
        var expiresOn = DateTimeOffset.UtcNow.AddDays(1);
        var expectedSasUri = "https://test.file.core.windows.net/testshare/test/file.txt?sv=2021-12-02&sr=f&sig=test&sp=rw";
        
        _mockClientWrapper.GetFileClientAsync(path, Arg.Any<CancellationToken>()).Returns(_mockFileClient);
        _mockFileClient.ExistsAsync(Arg.Any<CancellationToken>()).Returns(true);
        _mockFileClient.GenerateSasUriAsync(expiresOn, "rw", Arg.Any<CancellationToken>()).Returns(expectedSasUri);

        // Act
        var result = await _service.GenerateSasTokenAsync(path, expiresOn, "rw");

        // Assert
        Assert.Equal(expectedSasUri, result);
        await _mockFileClient.Received(1).GenerateSasUriAsync(expiresOn, "rw", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateSasTokenAsync_WithNullPath_ShouldThrowArgumentException()
    {
        // Arrange
        var expiresOn = DateTimeOffset.UtcNow.AddHours(1);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.GenerateSasTokenAsync(null!, expiresOn));
    }

    [Fact]
    public async Task GenerateSasTokenAsync_WithEmptyPath_ShouldThrowArgumentException()
    {
        // Arrange
        var expiresOn = DateTimeOffset.UtcNow.AddHours(1);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GenerateSasTokenAsync("", expiresOn));
    }

    [Fact]
    public async Task GenerateSasTokenAsync_WithNullPermissions_ShouldThrowArgumentNullException()
    {
        // Arrange
        var path = "test/file.txt";
        var expiresOn = DateTimeOffset.UtcNow.AddHours(1);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.GenerateSasTokenAsync(path, expiresOn, null!));
    }

    [Fact]
    public async Task GenerateSasTokenAsync_WithEmptyPermissions_ShouldThrowArgumentException()
    {
        // Arrange
        var path = "test/file.txt";
        var expiresOn = DateTimeOffset.UtcNow.AddHours(1);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GenerateSasTokenAsync(path, expiresOn, ""));
    }

    [Fact]
    public async Task GenerateSasTokenAsync_WithPastExpirationDate_ShouldThrowArgumentException()
    {
        // Arrange
        var path = "test/file.txt";
        var expiresOn = DateTimeOffset.UtcNow.AddHours(-1); // Past date

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.GenerateSasTokenAsync(path, expiresOn));
        Assert.Contains("Expiration date must be in the future", exception.Message);
    }

    [Fact]
    public async Task GenerateSasTokenAsync_WithZeroDuration_ShouldThrowArgumentException()
    {
        // Arrange
        var path = "test/file.txt";
        var duration = TimeSpan.Zero;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.GenerateSasTokenAsync(path, duration));
        Assert.Contains("Duration must be greater than zero", exception.Message);
    }

    [Fact]
    public async Task GenerateSasTokenAsync_WithNegativeDuration_ShouldThrowArgumentException()
    {
        // Arrange
        var path = "test/file.txt";
        var duration = TimeSpan.FromHours(-1);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.GenerateSasTokenAsync(path, duration));
        Assert.Contains("Duration must be greater than zero", exception.Message);
    }

    [Fact]
    public async Task GenerateSasTokenAsync_WhenFileDoesNotExist_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var path = "test/nonexistent.txt";
        var expiresOn = DateTimeOffset.UtcNow.AddHours(1);
        
        _mockClientWrapper.GetFileClientAsync(path, Arg.Any<CancellationToken>()).Returns(_mockFileClient);
        _mockFileClient.ExistsAsync(Arg.Any<CancellationToken>()).Returns(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FileNotFoundException>(() => _service.GenerateSasTokenAsync(path, expiresOn));
        Assert.Contains("File not found at path", exception.Message);
        Assert.Contains("Cannot generate SAS token for non-existent file", exception.Message);
    }

    [Fact]
    public async Task GenerateSasTokenAsync_WhenClientWrapperThrows_ShouldThrowFileStorageException()
    {
        // Arrange
        var path = "test/file.txt";
        var expiresOn = DateTimeOffset.UtcNow.AddHours(1);
        
        _mockClientWrapper.GetFileClientAsync(path, Arg.Any<CancellationToken>()).ThrowsAsync(new Exception("Test exception"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FileStorageException>(() => _service.GenerateSasTokenAsync(path, expiresOn));
        Assert.Contains("Failed to generate SAS token", exception.Message);
    }

    [Fact]
    public async Task GenerateSasTokenAsync_WithCancellationToken_ShouldPassTokenToClient()
    {
        // Arrange
        var path = "test/file.txt";
        var expiresOn = DateTimeOffset.UtcNow.AddHours(1);
        var cancellationToken = new CancellationToken(true); // Cancelled token
        
        _mockClientWrapper.GetFileClientAsync(path, cancellationToken).ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => _service.GenerateSasTokenAsync(path, expiresOn, "r", cancellationToken));
    }

    #endregion
}

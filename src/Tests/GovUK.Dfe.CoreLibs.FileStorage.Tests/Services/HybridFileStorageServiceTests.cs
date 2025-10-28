using GovUK.Dfe.CoreLibs.FileStorage.Services;
using GovUK.Dfe.CoreLibs.FileStorage.Settings;
using GovUK.Dfe.CoreLibs.FileStorage.Exceptions;
using Xunit;

namespace GovUK.Dfe.CoreLibs.FileStorage.Tests.Services;

public class HybridFileStorageServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly FileStorageOptions _validOptions;

    public HybridFileStorageServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "HybridTestFileStorage", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        _validOptions = new FileStorageOptions
        {
            Provider = "Hybrid",
            Local = new LocalFileStorageOptions
            {
                BaseDirectory = _testDirectory,
                CreateDirectoryIfNotExists = true,
                AllowOverwrite = true,
                MaxFileSizeBytes = 100 * 1024 * 1024 // 100MB
            },
            Azure = new AzureFileStorageOptions
            {
                ConnectionString = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=dGVzdA==;EndpointSuffix=core.windows.net",
                ShareName = "testshare"
            }
        };
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            // Give time for any file streams to close
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    Directory.Delete(_testDirectory, true);
                    break;
                }
                catch (IOException)
                {
                    if (i == 2) throw;
                    System.Threading.Thread.Sleep(100);
                }
            }
        }
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidOptions_ShouldNotThrow()
    {
        // Act & Assert
        var service = new HybridFileStorageService(_validOptions);
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new HybridFileStorageService((FileStorageOptions)null!));
    }

    [Fact]
    public void Constructor_WithMissingAzureConnectionString_ShouldThrowFileStorageConfigurationException()
    {
        // Arrange
        var options = new FileStorageOptions
        {
            Provider = "Hybrid",
            Local = new LocalFileStorageOptions
            {
                BaseDirectory = _testDirectory
            },
            Azure = new AzureFileStorageOptions
            {
                ConnectionString = "",
                ShareName = "testshare"
            }
        };

        // Act & Assert
        var exception = Assert.Throws<FileStorageConfigurationException>(() => new HybridFileStorageService(options));
        Assert.Contains("Azure connection string is required for hybrid mode", exception.Message);
    }

    [Fact]
    public void Constructor_WithMissingAzureShareName_ShouldThrowFileStorageConfigurationException()
    {
        // Arrange
        var options = new FileStorageOptions
        {
            Provider = "Hybrid",
            Local = new LocalFileStorageOptions
            {
                BaseDirectory = _testDirectory
            },
            Azure = new AzureFileStorageOptions
            {
                ConnectionString = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=test;EndpointSuffix=core.windows.net",
                ShareName = ""
            }
        };

        // Act & Assert
        var exception = Assert.Throws<FileStorageConfigurationException>(() => new HybridFileStorageService(options));
        Assert.Contains("Azure share name is required for hybrid mode", exception.Message);
    }

    #endregion

    #region File Operations - Local Storage

    [Fact]
    public async Task UploadAsync_WithValidParameters_ShouldSucceed()
    {
        // Arrange
        var service = new HybridFileStorageService(_validOptions);
        var path = "test/file.txt";
        var content = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });

        // Act
        await service.UploadAsync(path, content);

        // Assert - File should exist locally
        var fullPath = Path.Combine(_testDirectory, path);
        Assert.True(File.Exists(fullPath));
    }

    [Fact]
    public async Task DownloadAsync_WithExistingFile_ShouldSucceed()
    {
        // Arrange
        var service = new HybridFileStorageService(_validOptions);
        var path = "test/file.txt";
        var testData = new byte[] { 1, 2, 3, 4, 5 };
        
        // Create file first
        var fullPath = Path.Combine(_testDirectory, path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllBytesAsync(fullPath, testData);

        // Act
        using var result = await service.DownloadAsync(path);

        // Assert
        Assert.NotNull(result);
        var memStream = new MemoryStream();
        await result.CopyToAsync(memStream);
        Assert.Equal(testData, memStream.ToArray());
    }

    [Fact]
    public async Task DeleteAsync_WithExistingFile_ShouldSucceed()
    {
        // Arrange
        var service = new HybridFileStorageService(_validOptions);
        var path = "test/file.txt";
        
        // Create file first
        var fullPath = Path.Combine(_testDirectory, path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllBytesAsync(fullPath, new byte[] { 1, 2, 3 });

        // Act
        await service.DeleteAsync(path);

        // Assert
        Assert.False(File.Exists(fullPath));
    }

    [Fact]
    public async Task ExistsAsync_WithExistingFile_ShouldReturnTrue()
    {
        // Arrange
        var service = new HybridFileStorageService(_validOptions);
        var path = "test/file.txt";
        
        // Create file first
        var fullPath = Path.Combine(_testDirectory, path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllBytesAsync(fullPath, new byte[] { 1, 2, 3 });

        // Act
        var result = await service.ExistsAsync(path);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentFile_ShouldReturnFalse()
    {
        // Arrange
        var service = new HybridFileStorageService(_validOptions);
        var path = "test/nonexistent.txt";

        // Act
        var result = await service.ExistsAsync(path);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Integration Scenarios

    [Fact]
    public async Task HybridScenario_UploadLocallyAndVerifyFileExists_ShouldUseLocalStorage()
    {
        // Arrange
        var service = new HybridFileStorageService(_validOptions);
        var path = "documents/test.pdf";
        var content = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });

        // Act - Upload locally
        await service.UploadAsync(path, content);

        // Act - Check if file exists
        var exists = await service.ExistsAsync(path);

        // Assert
        Assert.True(exists);
        var fullPath = Path.Combine(_testDirectory, path);
        Assert.True(File.Exists(fullPath));
    }

    [Fact]
    public async Task HybridScenario_UploadAndDownload_ShouldWorkCorrectly()
    {
        // Arrange
        var service = new HybridFileStorageService(_validOptions);
        var path = "documents/test.pdf";
        var testData = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        var content = new MemoryStream(testData);

        // Act - Upload
        await service.UploadAsync(path, content);

        // Act - Download
        using var downloadedStream = await service.DownloadAsync(path);
        var memStream = new MemoryStream();
        await downloadedStream.CopyToAsync(memStream);

        // Assert
        Assert.Equal(testData, memStream.ToArray());
    }

    [Fact]
    public async Task HybridScenario_UploadDeleteAndVerify_ShouldWorkCorrectly()
    {
        // Arrange
        var service = new HybridFileStorageService(_validOptions);
        var path = "documents/test.pdf";
        var content = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });

        // Act - Upload
        await service.UploadAsync(path, content);
        Assert.True(await service.ExistsAsync(path));

        // Act - Delete
        await service.DeleteAsync(path);

        // Assert
        Assert.False(await service.ExistsAsync(path));
    }

    #endregion

    #region Interface Implementation

    [Fact]
    public void HybridService_ShouldImplementIFileStorageService()
    {
        // Arrange & Act
        var service = new HybridFileStorageService(_validOptions);

        // Assert
        Assert.IsAssignableFrom<GovUK.Dfe.CoreLibs.FileStorage.Interfaces.IFileStorageService>(service);
    }

    [Fact]
    public void HybridService_ShouldImplementIAzureSpecificOperations()
    {
        // Arrange & Act
        var service = new HybridFileStorageService(_validOptions);

        // Assert
        Assert.IsAssignableFrom<GovUK.Dfe.CoreLibs.FileStorage.Interfaces.IAzureSpecificOperations>(service);
    }

    #endregion
}


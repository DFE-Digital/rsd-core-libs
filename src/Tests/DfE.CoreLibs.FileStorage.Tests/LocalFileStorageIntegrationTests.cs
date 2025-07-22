using DfE.CoreLibs.FileStorage;
using DfE.CoreLibs.FileStorage.Exceptions;
using DfE.CoreLibs.FileStorage.Interfaces;
using DfE.CoreLibs.FileStorage.Services;
using DfE.CoreLibs.FileStorage.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DfE.CoreLibs.FileStorage.Tests;

public class LocalFileStorageIntegrationTests : IDisposable
{
    private readonly string _testBaseDirectory;
    private readonly IServiceProvider _serviceProvider;

    public LocalFileStorageIntegrationTests()
    {
        _testBaseDirectory = Path.Combine(Path.GetTempPath(), "LocalFileStorageIntegrationTests", Guid.NewGuid().ToString());
        
        var services = new ServiceCollection();
        var configuration = TestHelpers.CreateValidLocalConfigurationSettings(_testBaseDirectory);
        var config = TestHelpers.CreateConfiguration(configuration);
        
        services.AddFileStorage(config);
        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        // Clean up test directory with retry logic
        if (Directory.Exists(_testBaseDirectory))
        {
            try
            {
                // Force delete all files and directories
                Directory.Delete(_testBaseDirectory, true);
            }
            catch (IOException)
            {
                // If files are still in use, try again after a short delay
                try
                {
                    Thread.Sleep(100);
                    Directory.Delete(_testBaseDirectory, true);
                }
                catch (Exception)
                {
                    // If cleanup still fails, just log it but don't throw
                    // This prevents test failures due to cleanup issues
                }
            }
            catch (Exception)
            {
                // Catch any other exceptions during cleanup
                // This prevents test failures due to cleanup issues
            }
        }
    }

    [Fact]
    public void ServiceRegistration_ShouldRegisterLocalFileStorageService()
    {
        // Act
        var fileStorageService = _serviceProvider.GetService<IFileStorageService>();
        var options = _serviceProvider.GetService<FileStorageOptions>();

        // Assert
        Assert.NotNull(fileStorageService);
        Assert.IsType<LocalFileStorageService>(fileStorageService);
        Assert.NotNull(options);
        Assert.Equal("Local", options.Provider);
        Assert.Equal(_testBaseDirectory, options.Local.BaseDirectory);
    }

    [Fact]
    public async Task CompleteWorkflow_UploadDownloadDelete_ShouldWorkCorrectly()
    {
        // Arrange
        var fileStorageService = _serviceProvider.GetService<IFileStorageService>();
        var path = "test/document.txt";
        var content = "This is a test document with some content.";

        // Act & Assert - Upload
        using (var uploadStream = TestHelpers.CreateTestStream(content))
        {
            await fileStorageService.UploadAsync(path, uploadStream);
        }

        // Verify file exists
        Assert.True(await fileStorageService.ExistsAsync(path));

        // Act & Assert - Download
        using (var downloadStream = await fileStorageService.DownloadAsync(path))
        {
            var downloadedContent = TestHelpers.ReadStreamContent(downloadStream);
            Assert.Equal(content, downloadedContent);
        }

        // Act & Assert - Delete
        await fileStorageService.DeleteAsync(path);
        Assert.False(await fileStorageService.ExistsAsync(path));
    }

    [Fact]
    public async Task UploadMultipleFiles_ShouldCreateDirectoryStructure()
    {
        // Arrange
        var fileStorageService = _serviceProvider.GetService<IFileStorageService>();
        var files = new[]
        {
            ("documents/report1.txt", "Report 1 content"),
            ("documents/report2.txt", "Report 2 content"),
            ("images/photo1.jpg", "Fake image data"),
            ("images/photo2.jpg", "More fake image data")
        };

        // Act
        foreach (var (path, content) in files)
        {
            using var stream = TestHelpers.CreateTestStream(content);
            await fileStorageService.UploadAsync(path, stream);
        }

        // Assert
        foreach (var (path, expectedContent) in files)
        {
            Assert.True(await fileStorageService.ExistsAsync(path));
            
            using var downloadStream = await fileStorageService.DownloadAsync(path);
            var actualContent = TestHelpers.ReadStreamContent(downloadStream);
            Assert.Equal(expectedContent, actualContent);
        }

        // Verify directory structure was created
        Assert.True(Directory.Exists(Path.Combine(_testBaseDirectory, "documents")));
        Assert.True(Directory.Exists(Path.Combine(_testBaseDirectory, "images")));
    }

    [Fact]
    public async Task UploadLargeFile_ShouldHandleCorrectly()
    {
        // Arrange
        var fileStorageService = _serviceProvider.GetService<IFileStorageService>();
        var path = "large/data.bin";
        var largeContent = new byte[1024 * 1024]; // 1MB
        new Random(42).NextBytes(largeContent); // Use seed for deterministic test

        // Act
        using (var uploadStream = new MemoryStream(largeContent))
        {
            await fileStorageService.UploadAsync(path, uploadStream);
        }

        // Assert
        Assert.True(await fileStorageService.ExistsAsync(path));
        
        using (var downloadStream = await fileStorageService.DownloadAsync(path))
        {
            var downloadedBytes = new byte[largeContent.Length];
            await downloadStream.ReadAsync(downloadedBytes, 0, downloadedBytes.Length);
            Assert.Equal(largeContent, downloadedBytes);
        }
    }

    [Fact]
    public async Task UploadWithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var fileStorageService = _serviceProvider.GetService<IFileStorageService>();
        var path = "special/文件 with spaces (and) brackets.txt";
        var content = "Content with special characters: ñáéíóú 世界 🌍";

        // Act
        using (var uploadStream = TestHelpers.CreateTestStream(content))
        {
            await fileStorageService.UploadAsync(path, uploadStream);
        }

        // Assert
        Assert.True(await fileStorageService.ExistsAsync(path));
        
        using (var downloadStream = await fileStorageService.DownloadAsync(path))
        {
            var downloadedContent = TestHelpers.ReadStreamContent(downloadStream);
            Assert.Equal(content, downloadedContent);
        }
    }

    [Fact]
    public async Task ConcurrentUploads_ShouldNotInterfere()
    {
        // Arrange
        var fileStorageService = _serviceProvider.GetService<IFileStorageService>();
        var tasks = new List<Task>();
        var semaphore = new SemaphoreSlim(5); // Allow 5 concurrent uploads
        var uploadedFiles = new List<string>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            var fileIndex = i; // Capture the loop variable
            tasks.Add(Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var uniqueId = Guid.NewGuid().ToString("N")[..8]; // Generate unique ID
                    var path = $"concurrent/file{fileIndex}_{uniqueId}.txt";
                    var content = $"Content for file {fileIndex} with ID {uniqueId}";
                    using var stream = TestHelpers.CreateTestStream(content);
                    await fileStorageService.UploadAsync(path, stream);
                    
                    lock (uploadedFiles)
                    {
                        uploadedFiles.Add(path);
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(10, uploadedFiles.Count);
        foreach (var path in uploadedFiles)
        {
            Assert.True(await fileStorageService.ExistsAsync(path));
            
            using var downloadStream = await fileStorageService.DownloadAsync(path);
            var content = TestHelpers.ReadStreamContent(downloadStream);
            Assert.Contains("Content for file", content);
        }
    }

    [Fact]
    public async Task UploadWithCustomSettings_ShouldRespectConfiguration()
    {
        // Arrange
        var customDirectory = Path.Combine(Path.GetTempPath(), "CustomTestDir", Guid.NewGuid().ToString());
        var services = new ServiceCollection();
        var configuration = TestHelpers.CreateConfiguration(new Dictionary<string, string>
        {
            ["FileStorage:Provider"] = "Local",
            ["FileStorage:Local:BaseDirectory"] = customDirectory,
            ["FileStorage:Local:CreateDirectoryIfNotExists"] = "true",
            ["FileStorage:Local:AllowOverwrite"] = "false",
            ["FileStorage:Local:MaxFileSizeBytes"] = "1024", // 1KB limit
            ["FileStorage:Local:AllowedExtensions:0"] = "jpg",
            ["FileStorage:Local:AllowedExtensions:1"] = "png",
            ["FileStorage:Local:AllowedExtensions:2"] = "pdf"
        });

        services.AddFileStorage(configuration);
        var serviceProvider = services.BuildServiceProvider();
        var fileStorageService = serviceProvider.GetService<IFileStorageService>();

        try
        {
            // Act & Assert - Test overwrite disabled
            var path = "test.jpg";
            var content1 = "First content";
            var content2 = "Second content";

            using (var stream1 = TestHelpers.CreateTestStream(content1))
            {
                await fileStorageService.UploadAsync(path, stream1);
            }

            using (var stream2 = TestHelpers.CreateTestStream(content2))
            {
                var exception = await Assert.ThrowsAsync<FileStorageException>(() => 
                    fileStorageService.UploadAsync(path, stream2));
                Assert.Contains("already exists and overwrite is not allowed", exception.Message);
            }

            // Test file size limit
            var largeContent = new byte[2048]; // 2KB
            using (var largeStream = new MemoryStream(largeContent))
            {
                var exception = await Assert.ThrowsAsync<FileStorageException>(() => 
                    fileStorageService.UploadAsync("large.jpg", largeStream));
                Assert.Contains("exceeds maximum allowed size", exception.Message);
            }

            // Test allowed extensions
            using (var validStream = TestHelpers.CreateTestStream("valid content"))
            {
                await fileStorageService.UploadAsync("image.png", validStream);
                await fileStorageService.UploadAsync("document.pdf", validStream);
            }

            using (var invalidStream = TestHelpers.CreateTestStream("invalid content"))
            {
                var exception = await Assert.ThrowsAsync<FileStorageException>(() => 
                    fileStorageService.UploadAsync("file.txt", invalidStream));
                Assert.Contains("File extension 'txt' is not allowed", exception.Message);
                Assert.Contains("jpg, png, pdf", exception.Message);
            }
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(customDirectory))
            {
                Directory.Delete(customDirectory, true);
            }
        }
    }

    [Fact]
    public async Task UploadWithAllowedExtensions_ShouldValidateExtensionsCorrectly()
    {
        // Arrange
        var allowedExtensions = new[] { "jpg", "png", "pdf", "docx" };
        var services = new ServiceCollection();
        var configuration = TestHelpers.CreateValidLocalConfigurationSettings(_testBaseDirectory, allowedExtensions);
        var config = TestHelpers.CreateConfiguration(configuration);
        
        services.AddFileStorage(config);
        var serviceProvider = services.BuildServiceProvider();
        var fileStorageService = serviceProvider.GetService<IFileStorageService>();

        // Act & Assert - Test valid extensions
        var validFiles = new[]
        {
            ("image.jpg", "Image content"),
            ("document.pdf", "PDF content"),
            ("photo.PNG", "PNG content"), // Case insensitive
            ("report.DOCX", "DOCX content") // Case insensitive
        };

        foreach (var (path, content) in validFiles)
        {
            using var stream = TestHelpers.CreateTestStream(content);
            await fileStorageService.UploadAsync(path, stream);
            Assert.True(await fileStorageService.ExistsAsync(path));
        }

        // Test invalid extensions
        var invalidFiles = new[]
        {
            ("script.exe", "Executable content"),
            ("document.txt", "Text content"),
            ("file.bat", "Batch content"),
            ("filename", "No extension content")
        };

        foreach (var (path, content) in invalidFiles)
        {
            using var stream = TestHelpers.CreateTestStream(content);
            var exception = await Assert.ThrowsAsync<FileStorageException>(() => 
                fileStorageService.UploadAsync(path, stream));
            
            // Check for either "is not allowed" (invalid extension) or "extension is required" (no extension)
            Assert.True(
                exception.Message.Contains("is not allowed") || 
                exception.Message.Contains("extension is required"),
                $"Unexpected error message: {exception.Message}"
            );
        }
    }
} 
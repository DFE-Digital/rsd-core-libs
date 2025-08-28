using DfE.CoreLibs.FileStorage.Services;
using DfE.CoreLibs.FileStorage.Settings;
using DfE.CoreLibs.FileStorage.Exceptions;
using Xunit;
using FileNotFoundException = DfE.CoreLibs.FileStorage.Exceptions.FileNotFoundException;

namespace DfE.CoreLibs.FileStorage.Tests.Services;

public class LocalFileStorageServiceTests : IDisposable
{
    private readonly string _testBaseDirectory;
    private readonly LocalFileStorageService _service;
    private readonly FileStorageOptions _validOptions;

    public LocalFileStorageServiceTests()
    {
        _testBaseDirectory = Path.Combine(Path.GetTempPath(), "LocalFileStorageTests", Guid.NewGuid().ToString());
        _service = new LocalFileStorageService(_testBaseDirectory);

        _validOptions = new FileStorageOptions
        {
            Provider = "Local",
            Local = new LocalFileStorageOptions
            {
                BaseDirectory = _testBaseDirectory,
                CreateDirectoryIfNotExists = true,
                AllowOverwrite = true,
                MaxFileSizeBytes = 100 * 1024 * 1024 // 100MB
            }
        };
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
    public void Constructor_WithValidOptions_ShouldNotThrow()
    {
        // Act & Assert
        var service = new LocalFileStorageService(_validOptions);
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new LocalFileStorageService((FileStorageOptions)null!));
    }

    [Fact]
    public void Constructor_WithCustomBaseDirectory_ShouldCreateDirectory()
    {
        // Arrange
        var customDirectory = Path.Combine(Path.GetTempPath(), "CustomTestDir", Guid.NewGuid().ToString());
        var options = new FileStorageOptions
        {
            Provider = "Local",
            Local = new LocalFileStorageOptions
            {
                BaseDirectory = customDirectory,
                CreateDirectoryIfNotExists = true
            }
        };

        try
        {
            // Act
            var service = new LocalFileStorageService(options);

            // Assert
            Assert.True(Directory.Exists(customDirectory));
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
    public void Constructor_WithNonExistentDirectoryAndCreateDisabled_ShouldThrowException()
    {
        // Arrange
        var nonExistentDirectory = Path.Combine(Path.GetTempPath(), "NonExistentDir", Guid.NewGuid().ToString());
        var options = new FileStorageOptions
        {
            Provider = "Local",
            Local = new LocalFileStorageOptions
            {
                BaseDirectory = nonExistentDirectory,
                CreateDirectoryIfNotExists = false
            }
        };

        // Act & Assert
        var exception = Assert.Throws<FileStorageConfigurationException>(() => new LocalFileStorageService(options));
        Assert.Contains("does not exist and cannot be created", exception.Message);
    }

    [Fact]
    public async Task UploadAsync_WithValidParameters_ShouldSucceed()
    {
        // Arrange
        var path = "test/file.txt";
        var content = "Hello, World!";
        var stream = TestHelpers.CreateTestStream(content);

        // Act
        await _service.UploadAsync(path, stream);

        // Assert
        var fullPath = Path.Combine(_testBaseDirectory, path);
        Assert.True(File.Exists(fullPath));
        var fileContent = await File.ReadAllTextAsync(fullPath);
        Assert.Equal(content, fileContent);
    }

    [Fact]
    public async Task UploadAsync_WithNullPath_ShouldThrowArgumentException()
    {
        // Arrange
        var content = TestHelpers.CreateTestStream("test");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.UploadAsync(null!, content));
    }

    [Fact]
    public async Task UploadAsync_WithEmptyPath_ShouldThrowArgumentException()
    {
        // Arrange
        var content = TestHelpers.CreateTestStream("test");

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
        var nonReadableStream = new MemoryStream();
        nonReadableStream.Close(); // Make it non-readable

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.UploadAsync("test.txt", nonReadableStream));
    }

    [Fact]
    public async Task UploadAsync_WithDirectoryTraversal_ShouldThrowFileStorageException()
    {
        // Arrange
        var content = TestHelpers.CreateTestStream("test");
        var maliciousPath = "../../../etc/passwd";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FileStorageException>(() => _service.UploadAsync(maliciousPath, content));
        Assert.Contains("outside the allowed base directory", exception.Message);
    }

    [Fact]
    public async Task UploadAsync_WithLargeFile_ShouldThrowFileStorageException()
    {
        // Arrange
        var largeContent = new byte[101 * 1024 * 1024]; // 101MB
        var stream = new MemoryStream(largeContent);
        var service = new LocalFileStorageService(_testBaseDirectory, maxFileSizeBytes: 100 * 1024 * 1024); // 100MB limit

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FileStorageException>(() => service.UploadAsync("large.txt", stream));
        Assert.Contains("exceeds maximum allowed size", exception.Message);
    }

    [Fact]
    public async Task UploadAsync_WithOverwriteDisabled_ShouldThrowFileStorageException()
    {
        // Arrange
        var content1 = TestHelpers.CreateTestStream("First content");
        var content2 = TestHelpers.CreateTestStream("Second content");
        var service = new LocalFileStorageService(_testBaseDirectory, allowOverwrite: false);

        // Upload first file
        await service.UploadAsync("test.txt", content1);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FileStorageException>(() => service.UploadAsync("test.txt", content2));
        Assert.Contains("already exists and overwrite is not allowed", exception.Message);
    }

    [Fact]
    public async Task UploadAsync_WithDirectoryCreationDisabled_ShouldThrowFileStorageException()
    {
        // Arrange
        var content = TestHelpers.CreateTestStream("test");
        var service = new LocalFileStorageService(_testBaseDirectory, createDirectoryIfNotExists: false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FileStorageException>(() => service.UploadAsync("subdir/test.txt", content));
        Assert.Contains("does not exist and directory creation is disabled", exception.Message);
    }

    [Fact]
    public async Task DownloadAsync_WithValidParameters_ShouldSucceed()
    {
        // Arrange
        var path = "test/file.txt";
        var expectedContent = "Hello, World!";
        var fullPath = Path.Combine(_testBaseDirectory, path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(fullPath, expectedContent);

        // Act
        var result = await _service.DownloadAsync(path);

        // Assert
        Assert.NotNull(result);
        var actualContent = TestHelpers.ReadStreamContent(result);
        Assert.Equal(expectedContent, actualContent);
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
        // Act & Assert
        var exception = await Assert.ThrowsAsync<FileNotFoundException>(() => _service.DownloadAsync("nonexistent.txt"));
        Assert.Contains("File not found at path", exception.Message);
    }

    [Fact]
    public async Task DeleteAsync_WithValidParameters_ShouldSucceed()
    {
        // Arrange
        var path = "test/file.txt";
        var fullPath = Path.Combine(_testBaseDirectory, path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(fullPath, "test content");

        // Act
        await _service.DeleteAsync(path);

        // Assert
        Assert.False(File.Exists(fullPath));
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
    public async Task DeleteAsync_WhenFileDoesNotExist_ShouldNotThrow()
    {
        // Act & Assert
        await _service.DeleteAsync("nonexistent.txt"); // Should not throw
    }

    [Fact]
    public async Task ExistsAsync_WithValidParameters_ShouldReturnTrue()
    {
        // Arrange
        var path = "test/file.txt";
        var fullPath = Path.Combine(_testBaseDirectory, path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(fullPath, "test content");

        // Act
        var result = await _service.ExistsAsync(path);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_WithValidParameters_ShouldReturnFalse()
    {
        // Act
        var result = await _service.ExistsAsync("nonexistent.txt");

        // Assert
        Assert.False(result);
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
    public async Task AllMethods_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var content = TestHelpers.CreateTestStream("test content");
        await _service.UploadAsync("test.txt", content);

        // Test that cancellation tokens are properly passed through
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - Operations should respect cancellation tokens
        // Even if they complete immediately, they should check the token
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _service.DownloadAsync("test.txt", cts.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _service.ExistsAsync("test.txt", cts.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _service.DeleteAsync("test.txt", cts.Token));
    }

    [Fact]
    public async Task UploadAsync_WithCancellationToken_ShouldPassTokenThrough()
    {
        // Arrange
        var content = TestHelpers.CreateTestStream("test content");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - Upload should respect cancellation token
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _service.UploadAsync("test.txt", content, cts.Token));
    }

    [Fact]
    public async Task UploadAsync_WithSubdirectories_ShouldCreateDirectories()
    {
        // Arrange
        var path = "subdir1/subdir2/file.txt";
        var content = TestHelpers.CreateTestStream("test content");

        // Act
        await _service.UploadAsync(path, content);

        // Assert
        var fullPath = Path.Combine(_testBaseDirectory, path);
        Assert.True(File.Exists(fullPath));
        var directory = Path.GetDirectoryName(fullPath);
        Assert.True(Directory.Exists(directory));
    }

    [Fact]
    public async Task UploadAsync_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var path = "test/file with spaces (and) brackets.txt";
        var content = TestHelpers.CreateTestStream("test content");

        // Act
        await _service.UploadAsync(path, content);

        // Assert
        var fullPath = Path.Combine(_testBaseDirectory, path);
        Assert.True(File.Exists(fullPath));
    }

    [Fact]
    public async Task DownloadAsync_WithLargeFile_ShouldHandleCorrectly()
    {
        // Arrange
        var path = "large.txt";
        var largeContent = new byte[1024 * 1024]; // 1MB
        new Random().NextBytes(largeContent);
        var fullPath = Path.Combine(_testBaseDirectory, path);
        await File.WriteAllBytesAsync(fullPath, largeContent);

        // Act
        var result = await _service.DownloadAsync(path);

        // Assert
        Assert.NotNull(result);
        var downloadedBytes = new byte[largeContent.Length];
        await result.ReadAsync(downloadedBytes, 0, downloadedBytes.Length);
        Assert.Equal(largeContent, downloadedBytes);
    }

    [Fact]
    public async Task ConcurrentUploads_ShouldNotInterfere()
    {
        // Arrange
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
                    var content = TestHelpers.CreateTestStream($"Content {fileIndex} with ID {uniqueId}");
                    await _service.UploadAsync(path, content);
                    
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
            Assert.True(await _service.ExistsAsync(path));
        }
    }

    [Fact]
    public async Task UploadAndDownload_WithUnicodeContent_ShouldPreserveEncoding()
    {
        // Arrange
        var path = "unicode.txt";
        var unicodeContent = "Hello, 世界! 🌍";
        var stream = TestHelpers.CreateTestStream(unicodeContent);

        // Act
        await _service.UploadAsync(path, stream);
        var result = await _service.DownloadAsync(path);

        // Assert
        var downloadedContent = TestHelpers.ReadStreamContent(result);
        Assert.Equal(unicodeContent, downloadedContent);
    }

    [Fact]
    public async Task UploadAsync_WithAllowedExtensions_ShouldAllowValidExtensions()
    {
        // Arrange
        var allowedExtensions = new[] { "jpg", "png", "pdf", "docx" };
        var service = new LocalFileStorageService(_testBaseDirectory, allowedExtensions: allowedExtensions);
        var content = TestHelpers.CreateTestStream("test content");

        // Act & Assert - Should allow valid extensions
        await service.UploadAsync("test.jpg", content);
        await service.UploadAsync("document.pdf", content);
        await service.UploadAsync("image.PNG", content); // Case insensitive
        await service.UploadAsync("file.DOCX", content); // Case insensitive

        // Verify files were created
        Assert.True(await service.ExistsAsync("test.jpg"));
        Assert.True(await service.ExistsAsync("document.pdf"));
        Assert.True(await service.ExistsAsync("image.PNG"));
        Assert.True(await service.ExistsAsync("file.DOCX"));
    }

    [Fact]
    public async Task UploadAsync_WithAllowedExtensions_ShouldRejectInvalidExtensions()
    {
        // Arrange
        var allowedExtensions = new[] { "jpg", "png", "pdf" };
        var service = new LocalFileStorageService(_testBaseDirectory, allowedExtensions: allowedExtensions);
        var content = TestHelpers.CreateTestStream("test content");

        // Act & Assert - Should reject invalid extensions
        var exception1 = await Assert.ThrowsAsync<FileStorageException>(() => 
            service.UploadAsync("test.txt", content));
        Assert.Contains("File extension 'txt' is not allowed", exception1.Message);
        Assert.Contains("jpg, png, pdf", exception1.Message);

        var exception2 = await Assert.ThrowsAsync<FileStorageException>(() => 
            service.UploadAsync("document.exe", content));
        Assert.Contains("File extension 'exe' is not allowed", exception2.Message);

        var exception3 = await Assert.ThrowsAsync<FileStorageException>(() => 
            service.UploadAsync("file.bat", content));
        Assert.Contains("File extension 'bat' is not allowed", exception3.Message);
    }

    [Fact]
    public async Task UploadAsync_WithAllowedExtensions_ShouldRejectFilesWithoutExtensions()
    {
        // Arrange
        var allowedExtensions = new[] { "jpg", "png", "pdf" };
        var service = new LocalFileStorageService(_testBaseDirectory, allowedExtensions: allowedExtensions);
        var content = TestHelpers.CreateTestStream("test content");

        // Act & Assert - Should reject files without extensions
        var exception = await Assert.ThrowsAsync<FileStorageException>(() => 
            service.UploadAsync("filename", content));
        Assert.Contains("File extension is required", exception.Message);
        Assert.Contains("jpg, png, pdf", exception.Message);
    }

    [Fact]
    public async Task UploadAsync_WithEmptyAllowedExtensions_ShouldAllowAllExtensions()
    {
        // Arrange
        var service = new LocalFileStorageService(_testBaseDirectory, allowedExtensions: new string[0]);
        var content = TestHelpers.CreateTestStream("test content");

        // Act & Assert - Should allow any extension when no restrictions are set
        await service.UploadAsync("test.txt", content);
        await service.UploadAsync("document.pdf", content);
        await service.UploadAsync("image.jpg", content);
        await service.UploadAsync("script.exe", content);
        await service.UploadAsync("filename", content); // No extension

        // Verify files were created
        Assert.True(await service.ExistsAsync("test.txt"));
        Assert.True(await service.ExistsAsync("document.pdf"));
        Assert.True(await service.ExistsAsync("image.jpg"));
        Assert.True(await service.ExistsAsync("script.exe"));
        Assert.True(await service.ExistsAsync("filename"));
    }

    [Fact]
    public async Task UploadAsync_WithNullAllowedExtensions_ShouldAllowAllExtensions()
    {
        // Arrange
        var service = new LocalFileStorageService(_testBaseDirectory, allowedExtensions: null);
        var content = TestHelpers.CreateTestStream("test content");

        // Act & Assert - Should allow any extension when no restrictions are set
        await service.UploadAsync("test.txt", content);
        await service.UploadAsync("document.pdf", content);
        await service.UploadAsync("image.jpg", content);
        await service.UploadAsync("script.exe", content);
        await service.UploadAsync("filename", content); // No extension

        // Verify files were created
        Assert.True(await service.ExistsAsync("test.txt"));
        Assert.True(await service.ExistsAsync("document.pdf"));
        Assert.True(await service.ExistsAsync("image.jpg"));
        Assert.True(await service.ExistsAsync("script.exe"));
        Assert.True(await service.ExistsAsync("filename"));
    }

    [Fact]
    public async Task UploadAsync_WithAllowedExtensions_ShouldHandleCaseInsensitiveValidation()
    {
        // Arrange
        var allowedExtensions = new[] { "JPG", "PNG", "PDF" };
        var service = new LocalFileStorageService(_testBaseDirectory, allowedExtensions: allowedExtensions);
        var content = TestHelpers.CreateTestStream("test content");

        // Act & Assert - Should allow case-insensitive matches
        await service.UploadAsync("test.jpg", content);
        await service.UploadAsync("image.PNG", content);
        await service.UploadAsync("document.Pdf", content);

        // Should reject non-matching extensions
        var exception = await Assert.ThrowsAsync<FileStorageException>(() => 
            service.UploadAsync("file.txt", content));
        Assert.Contains("File extension 'txt' is not allowed", exception.Message);
    }

    [Fact]
    public async Task UploadAsync_WithAllowedExtensions_ShouldHandleExtensionsWithMultipleDots()
    {
        // Arrange
        var allowedExtensions = new[] { "jpg", "png", "pdf" };
        var service = new LocalFileStorageService(_testBaseDirectory, allowedExtensions: allowedExtensions);
        var content = TestHelpers.CreateTestStream("test content");

        // Act & Assert - Should validate the last extension
        await service.UploadAsync("test.backup.jpg", content);
        await service.UploadAsync("document.final.pdf", content);

        var exception = await Assert.ThrowsAsync<FileStorageException>(() => 
            service.UploadAsync("file.backup.txt", content));
        Assert.Contains("File extension 'txt' is not allowed", exception.Message);
    }

    [Fact]
    public async Task UploadAsync_WithAllowedExtensions_ShouldHandleExtensionsWithLeadingDots()
    {
        // Arrange
        var allowedExtensions = new[] { "jpg", "png", "pdf" };
        var service = new LocalFileStorageService(_testBaseDirectory, allowedExtensions: allowedExtensions);
        var content = TestHelpers.CreateTestStream("test content");

        // Act & Assert - Should handle leading dots correctly
        await service.UploadAsync("test.jpg", content);
        await service.UploadAsync("image.png", content);

        var exception = await Assert.ThrowsAsync<FileStorageException>(() => 
            service.UploadAsync("file.txt", content));
        Assert.Contains("File extension 'txt' is not allowed", exception.Message);
    }

    [Fact]
    public void Constructor_WithAllowedExtensions_ShouldStoreExtensionsCorrectly()
    {
        // Arrange
        var allowedExtensions = new[] { "jpg", "png", "pdf" };
        var options = new FileStorageOptions
        {
            Provider = "Local",
            Local = new LocalFileStorageOptions
            {
                BaseDirectory = _testBaseDirectory,
                AllowedExtensions = allowedExtensions
            }
        };

        // Act
        var service = new LocalFileStorageService(options);

        // Assert - Test that the service correctly validates extensions
        var content = TestHelpers.CreateTestStream("test");
        
        // Should allow valid extensions
        service.UploadAsync("test.jpg", content).Wait();
        service.UploadAsync("image.png", content).Wait();
        
        // Should reject invalid extensions
        var exception = Assert.ThrowsAsync<FileStorageException>(() => 
            service.UploadAsync("file.txt", content)).Result;
        Assert.Contains("File extension 'txt' is not allowed", exception.Message);
    }

    [Fact]
    public async Task UploadAsync_WithAllowedFileNamePattern_ShouldAllowValidFileNames()
    {
        // Arrange
        var pattern = "^[a-zA-Z0-9_-]+$"; // Only letters, numbers, underscore, and hyphens
        var service = new LocalFileStorageService(_testBaseDirectory, allowedFileNamePattern: pattern);
        var content = TestHelpers.CreateTestStream("test content");

        // Act & Assert - Should allow valid filenames
        await service.UploadAsync("test.txt", content);
        await service.UploadAsync("Test_File.pdf", content);
        await service.UploadAsync("file-name.jpg", content);
        await service.UploadAsync("MyFile123.docx", content);
        await service.UploadAsync("FILE_NAME_2.png", content);
        await service.UploadAsync("a123_test-file.txt", content);

        // Verify files were created
        Assert.True(await service.ExistsAsync("test.txt"));
        Assert.True(await service.ExistsAsync("Test_File.pdf"));
        Assert.True(await service.ExistsAsync("file-name.jpg"));
        Assert.True(await service.ExistsAsync("MyFile123.docx"));
        Assert.True(await service.ExistsAsync("FILE_NAME_2.png"));
        Assert.True(await service.ExistsAsync("a123_test-file.txt"));
    }

    [Fact]
    public async Task UploadAsync_WithAllowedFileNamePattern_ShouldRejectInvalidFileNames()
    {
        // Arrange
        var pattern = "^[a-zA-Z0-9_-]+$"; // Only letters, numbers, underscore, and hyphens
        var service = new LocalFileStorageService(_testBaseDirectory, allowedFileNamePattern: pattern);
        var content = TestHelpers.CreateTestStream("test content");

        // Act & Assert - Should reject invalid filenames
        var exception1 = await Assert.ThrowsAsync<FileStorageException>(() => 
            service.UploadAsync("file with spaces.txt", content));
        Assert.Contains("does not match the allowed pattern", exception1.Message);
        Assert.Contains("file with spaces", exception1.Message);

        var exception2 = await Assert.ThrowsAsync<FileStorageException>(() => 
            service.UploadAsync("file@symbol.pdf", content));
        Assert.Contains("does not match the allowed pattern", exception2.Message);
        Assert.Contains("file@symbol", exception2.Message);

        var exception3 = await Assert.ThrowsAsync<FileStorageException>(() => 
            service.UploadAsync("file.with.dots.jpg", content));
        Assert.Contains("does not match the allowed pattern", exception3.Message);
        Assert.Contains("file.with.dots", exception3.Message);

        var exception4 = await Assert.ThrowsAsync<FileStorageException>(() => 
            service.UploadAsync("file%percent.txt", content));
        Assert.Contains("does not match the allowed pattern", exception4.Message);

        var exception5 = await Assert.ThrowsAsync<FileStorageException>(() => 
            service.UploadAsync("file(brackets).txt", content));
        Assert.Contains("does not match the allowed pattern", exception5.Message);
    }

    [Fact]
    public async Task UploadAsync_WithNullFileNamePattern_ShouldAllowAllFileNames()
    {
        // Arrange
        var service = new LocalFileStorageService(_testBaseDirectory, allowedFileNamePattern: null);
        var content = TestHelpers.CreateTestStream("test content");

        // Act & Assert - Should allow any filename when no pattern is set
        await service.UploadAsync("file with spaces.txt", content);
        await service.UploadAsync("file@symbol.pdf", content);
        await service.UploadAsync("file.with.dots.jpg", content);
        await service.UploadAsync("file%percent.txt", content);
        await service.UploadAsync("file(brackets).txt", content);
        await service.UploadAsync("file-name_123.txt", content);

        // Verify files were created
        Assert.True(await service.ExistsAsync("file with spaces.txt"));
        Assert.True(await service.ExistsAsync("file@symbol.pdf"));
        Assert.True(await service.ExistsAsync("file.with.dots.jpg"));
        Assert.True(await service.ExistsAsync("file%percent.txt"));
        Assert.True(await service.ExistsAsync("file(brackets).txt"));
        Assert.True(await service.ExistsAsync("file-name_123.txt"));
    }

    [Fact]
    public async Task UploadAsync_WithEmptyFileNamePattern_ShouldAllowAllFileNames()
    {
        // Arrange
        var service = new LocalFileStorageService(_testBaseDirectory, allowedFileNamePattern: "");
        var content = TestHelpers.CreateTestStream("test content");

        // Act & Assert - Should allow any filename when pattern is empty
        await service.UploadAsync("file with spaces.txt", content);
        await service.UploadAsync("file@symbol.pdf", content);
        await service.UploadAsync("file.with.dots.jpg", content);

        // Verify files were created
        Assert.True(await service.ExistsAsync("file with spaces.txt"));
        Assert.True(await service.ExistsAsync("file@symbol.pdf"));
        Assert.True(await service.ExistsAsync("file.with.dots.jpg"));
    }

    [Fact]
    public async Task UploadAsync_WithFileNamePattern_ShouldRejectFilesWithoutFileName()
    {
        // Arrange
        var pattern = "^[a-zA-Z0-9_-]+$";
        var service = new LocalFileStorageService(_testBaseDirectory, allowedFileNamePattern: pattern);
        var content = TestHelpers.CreateTestStream("test content");

        // Act & Assert - Should reject files with only extension
        var exception = await Assert.ThrowsAsync<FileStorageException>(() => 
            service.UploadAsync(".txt", content));
        Assert.Contains("Filename is required when filename pattern validation is enabled", exception.Message);
    }

    [Fact]
    public async Task UploadAsync_WithFileNamePattern_ShouldHandleSubdirectories()
    {
        // Arrange
        var pattern = "^[a-zA-Z0-9_-]+$";
        var service = new LocalFileStorageService(_testBaseDirectory, allowedFileNamePattern: pattern);
        var content = TestHelpers.CreateTestStream("test content");

        // Act & Assert - Should validate only the filename part, not directory path
        await service.UploadAsync("subdir/valid_file-name.txt", content);
        await service.UploadAsync("sub dir/valid_file.txt", content); // Space in directory is OK
        await service.UploadAsync("sub@dir/valid_file.txt", content); // Symbol in directory is OK

        var exception = await Assert.ThrowsAsync<FileStorageException>(() => 
            service.UploadAsync("subdir/invalid file name.txt", content));
        Assert.Contains("does not match the allowed pattern", exception.Message);
        Assert.Contains("invalid file name", exception.Message);

        // Verify valid files were created
        Assert.True(await service.ExistsAsync("subdir/valid_file-name.txt"));
        Assert.True(await service.ExistsAsync("sub dir/valid_file.txt"));
        Assert.True(await service.ExistsAsync("sub@dir/valid_file.txt"));
    }

    [Fact]
    public async Task UploadAsync_WithCustomFileNamePattern_ShouldRespectCustomPattern()
    {
        // Arrange
        var pattern = "^[A-Z]{2}[0-9]{3}$"; // Pattern for 2 uppercase letters followed by 3 digits
        var service = new LocalFileStorageService(_testBaseDirectory, allowedFileNamePattern: pattern);
        var content = TestHelpers.CreateTestStream("test content");

        // Act & Assert - Should allow files matching custom pattern
        await service.UploadAsync("AB123.txt", content);
        await service.UploadAsync("XY789.pdf", content);

        // Should reject files not matching custom pattern
        var exception1 = await Assert.ThrowsAsync<FileStorageException>(() => 
            service.UploadAsync("abc123.txt", content)); // lowercase letters
        Assert.Contains("does not match the allowed pattern", exception1.Message);

        var exception2 = await Assert.ThrowsAsync<FileStorageException>(() => 
            service.UploadAsync("AB1234.txt", content)); // too many digits
        Assert.Contains("does not match the allowed pattern", exception2.Message);

        var exception3 = await Assert.ThrowsAsync<FileStorageException>(() => 
            service.UploadAsync("A123.txt", content)); // too few letters
        Assert.Contains("does not match the allowed pattern", exception3.Message);

        // Verify valid files were created
        Assert.True(await service.ExistsAsync("AB123.txt"));
        Assert.True(await service.ExistsAsync("XY789.pdf"));
    }

    [Fact]
    public void Constructor_WithInvalidFileNamePattern_ShouldThrowFileStorageConfigurationException()
    {
        // Arrange
        var invalidPattern = "[unclosed"; // Invalid regex pattern
        var options = new FileStorageOptions
        {
            Provider = "Local",
            Local = new LocalFileStorageOptions
            {
                BaseDirectory = _testBaseDirectory,
                AllowedFileNamePattern = invalidPattern
            }
        };

        // Act & Assert
        var exception = Assert.Throws<FileStorageConfigurationException>(() => 
            new LocalFileStorageService(options));
        Assert.Contains("Invalid filename pattern", exception.Message);
        Assert.Contains(invalidPattern, exception.Message);
    }

    [Fact]
    public void Constructor_WithInvalidFileNamePattern_InternalConstructor_ShouldThrowFileStorageConfigurationException()
    {
        // Arrange
        var invalidPattern = "*+"; // Invalid regex pattern

        // Act & Assert
        var exception = Assert.Throws<FileStorageConfigurationException>(() => 
            new LocalFileStorageService(_testBaseDirectory, allowedFileNamePattern: invalidPattern));
        Assert.Contains("Invalid filename pattern", exception.Message);
        Assert.Contains(invalidPattern, exception.Message);
    }

    [Fact]
    public async Task UploadAsync_WithBothExtensionAndFileNamePattern_ShouldValidateBoth()
    {
        // Arrange
        var allowedExtensions = new[] { "txt", "pdf" };
        var pattern = "^[a-zA-Z0-9_-]+$";
        var options = new FileStorageOptions
        {
            Provider = "Local",
            Local = new LocalFileStorageOptions
            {
                BaseDirectory = _testBaseDirectory,
                AllowedExtensions = allowedExtensions,
                AllowedFileNamePattern = pattern
            }
        };
        var service = new LocalFileStorageService(options);
        var content = TestHelpers.CreateTestStream("test content");

        // Act & Assert - Should allow valid filename and extension
        await service.UploadAsync("valid_file.txt", content);
        await service.UploadAsync("another-file.pdf", content);

        // Should reject invalid extension even with valid filename
        var exception1 = await Assert.ThrowsAsync<FileStorageException>(() => 
            service.UploadAsync("valid_file.jpg", content));
        Assert.Contains("File extension 'jpg' is not allowed", exception1.Message);

        // Should reject invalid filename even with valid extension
        var exception2 = await Assert.ThrowsAsync<FileStorageException>(() => 
            service.UploadAsync("invalid file.txt", content));
        Assert.Contains("does not match the allowed pattern", exception2.Message);

        // Should reject both invalid filename and extension
        var exception3 = await Assert.ThrowsAsync<FileStorageException>(() => 
            service.UploadAsync("invalid file.jpg", content));
        // Extension validation comes first, so should get extension error
        Assert.Contains("File extension 'jpg' is not allowed", exception3.Message);

        // Verify valid files were created
        Assert.True(await service.ExistsAsync("valid_file.txt"));
        Assert.True(await service.ExistsAsync("another-file.pdf"));
    }
} 
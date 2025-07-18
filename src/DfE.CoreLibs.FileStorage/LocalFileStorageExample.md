# Local File Storage Usage Guide

This guide shows how to use the Local File Storage provider in the DfE.CoreLibs.FileStorage library.

## Configuration

### appsettings.json
```json
{
  "FileStorage": {
    "Provider": "Local",
    "Local": {
      "BaseDirectory": "C:\\FileStorage",
      "CreateDirectoryIfNotExists": true,
      "AllowOverwrite": true,
      "MaxFileSizeBytes": 104857600,
      "AllowedExtensions": ["jpg", "png", "pdf", "docx", "xlsx"]
    }
  }
}
```

### Program.cs or Startup.cs
```csharp
using DfE.CoreLibs.FileStorage;

var builder = WebApplication.CreateBuilder(args);

// Register file storage services
builder.Services.AddFileStorage(builder.Configuration);

var app = builder.Build();
```

## Usage Examples

### Basic File Operations

```csharp
public class FileController : ControllerBase
{
    private readonly IFileStorageService _fileStorageService;

    public FileController(IFileStorageService fileStorageService)
    {
        _fileStorageService = fileStorageService;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided");

        var path = $"uploads/{Guid.NewGuid()}_{file.FileName}";
        
        using var stream = file.OpenReadStream();
        await _fileStorageService.UploadAsync(path, stream);
        
        return Ok(new { Path = path });
    }

    [HttpGet("download/{path}")]
    public async Task<IActionResult> DownloadFile(string path)
    {
        if (!await _fileStorageService.ExistsAsync(path))
            return NotFound();

        var stream = await _fileStorageService.DownloadAsync(path);
        return File(stream, "application/octet-stream", Path.GetFileName(path));
    }

    [HttpDelete("delete/{path}")]
    public async Task<IActionResult> DeleteFile(string path)
    {
        if (!await _fileStorageService.ExistsAsync(path))
            return NotFound();

        await _fileStorageService.DeleteAsync(path);
        return NoContent();
    }

    [HttpGet("exists/{path}")]
    public async Task<IActionResult> CheckFileExists(string path)
    {
        var exists = await _fileStorageService.ExistsAsync(path);
        return Ok(new { Exists = exists });
    }
}
```

### File Extension Validation

The local file storage provider supports file extension validation to restrict uploads to specific file types:

```csharp
public class DocumentUploadController : ControllerBase
{
    private readonly IFileStorageService _fileStorageService;

    public DocumentUploadController(IFileStorageService fileStorageService)
    {
        _fileStorageService = fileStorageService;
    }

    [HttpPost("upload-document")]
    public async Task<IActionResult> UploadDocument(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided");

        try
        {
            var path = $"documents/{Guid.NewGuid()}_{file.FileName}";
            using var stream = file.OpenReadStream();
            await _fileStorageService.UploadAsync(path, stream);
            
            return Ok(new { Path = path, Message = "Document uploaded successfully" });
        }
        catch (FileStorageException ex) when (ex.Message.Contains("is not allowed"))
        {
            return BadRequest(new { Error = "File type not allowed", Details = ex.Message });
        }
        catch (FileStorageException ex) when (ex.Message.Contains("extension is required"))
        {
            return BadRequest(new { Error = "File extension is required", Details = ex.Message });
        }
    }
}
```

### Advanced Usage

```csharp
public class DocumentService
{
    private readonly IFileStorageService _fileStorageService;

    public DocumentService(IFileStorageService fileStorageService)
    {
        _fileStorageService = fileStorageService;
    }

    public async Task<string> SaveDocumentAsync(string fileName, byte[] content)
    {
        var path = $"documents/{DateTime.Now:yyyy/MM/dd}/{fileName}";
        
        using var stream = new MemoryStream(content);
        await _fileStorageService.UploadAsync(path, stream);
        
        return path;
    }

    public async Task<byte[]> GetDocumentAsync(string path)
    {
        if (!await _fileStorageService.ExistsAsync(path))
            throw new FileNotFoundException($"Document not found: {path}");

        using var stream = await _fileStorageService.DownloadAsync(path);
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        
        return memoryStream.ToArray();
    }

    public async Task DeleteDocumentAsync(string path)
    {
        if (await _fileStorageService.ExistsAsync(path))
        {
            await _fileStorageService.DeleteAsync(path);
        }
    }
}
```

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `BaseDirectory` | string | `{AppBase}/FileStorage` | Base directory where files will be stored |
| `CreateDirectoryIfNotExists` | bool | `true` | Whether to create the base directory if it doesn't exist |
| `AllowOverwrite` | bool | `true` | Whether to allow overwriting existing files during upload |
| `MaxFileSizeBytes` | long | `104857600` (100MB) | Maximum file size in bytes (0 to disable) |
| `AllowedExtensions` | string[] | `[]` (all allowed) | Array of allowed file extensions without the dot |

### Allowed Extensions Examples

```json
{
  "FileStorage": {
    "Provider": "Local",
    "Local": {
      "BaseDirectory": "C:\\FileStorage",
      "AllowedExtensions": ["jpg", "png", "pdf", "docx", "xlsx"]
    }
  }
}
```

**Note**: 
- If `AllowedExtensions` is empty or not specified, all file extensions are allowed
- Extensions are case-insensitive (e.g., "JPG" and "jpg" are treated the same)
- Files without extensions are rejected when `AllowedExtensions` is configured
- The validation checks the last extension in filenames with multiple dots (e.g., "file.backup.jpg" is validated as "jpg")

## Security Features

- **Directory Traversal Protection**: The service prevents access to files outside the base directory
- **Path Normalization**: All paths are normalized to prevent security issues
- **File Size Limits**: Configurable maximum file size to prevent abuse
- **File Extension Validation**: Restrict uploads to specific file types for security

## Error Handling

The service throws appropriate exceptions for different scenarios:

- `ArgumentNullException`: When required parameters are null
- `ArgumentException`: When parameters are invalid
- `FileStorageException`: When file operations fail or file extensions are not allowed
- `FileNotFoundException`: When trying to download a non-existent file

### File Extension Validation Errors

When file extension validation is enabled, the service will throw `FileStorageException` with specific messages:

- **No extension**: "File extension is required. Allowed extensions: jpg, png, pdf"
- **Invalid extension**: "File extension 'exe' is not allowed. Allowed extensions: jpg, png, pdf"

## Best Practices

1. **Use relative paths**: Always use relative paths within your storage structure
2. **Handle exceptions**: Always wrap file operations in try-catch blocks
3. **Dispose streams**: Always dispose of downloaded streams to free resources
4. **Validate file sizes**: Check file sizes before upload if needed
5. **Use appropriate file extensions**: Include file extensions in paths for proper MIME type detection
6. **Configure allowed extensions**: Restrict file types to prevent security issues
7. **Handle extension validation errors**: Provide user-friendly error messages for invalid file types

## Testing

The library includes comprehensive tests for the local file storage provider. You can run the tests to verify functionality:

```bash
dotnet test --filter "LocalFileStorage"
```

### Testing File Extension Validation

```csharp
[Fact]
public async Task Upload_WithAllowedExtensions_ShouldValidateExtensions()
{
    // Arrange
    var allowedExtensions = new[] { "jpg", "png", "pdf" };
    var service = new LocalFileStorageService(_testDirectory, allowedExtensions: allowedExtensions);
    var content = TestHelpers.CreateTestStream("test content");

    // Act & Assert - Valid extensions
    await service.UploadAsync("image.jpg", content);
    await service.UploadAsync("photo.PNG", content); // Case insensitive

    // Act & Assert - Invalid extensions
    var exception = await Assert.ThrowsAsync<FileStorageException>(() => 
        service.UploadAsync("script.exe", content));
    Assert.Contains("File extension 'exe' is not allowed", exception.Message);
}
``` 
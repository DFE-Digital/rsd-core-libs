# GovUK.Dfe.CoreLibs.FileStorage

This library provides a simple abstraction for file storage with support for three providers:
- **Local**: File system storage
- **Azure**: Azure File Service storage
- **Hybrid**: Combines local storage for file operations with Azure for SAS token generation

## Installation

```sh
dotnet add package GovUK.Dfe.CoreLibs.FileStorage
```

## Configuration

Add a `FileStorage` section to your `appsettings.json` based on your chosen provider:

### Local Storage

```json
"FileStorage": {
  "Provider": "Local",
  "Local": {
    "BaseDirectory": "C:\\FileStorage",
    "CreateDirectoryIfNotExists": true,
    "AllowOverwrite": true,
    "MaxFileSizeBytes": 104857600,
    "AllowedExtensions": ["pdf", "docx", "xlsx", "jpg", "png"],
    "AllowedFileNamePattern": "^[a-zA-Z0-9_-]+$"
  }
}
```

### Azure Storage

```json
"FileStorage": {
  "Provider": "Azure",
  "Azure": {
    "ConnectionString": "<storage-connection-string>",
    "ShareName": "<share-name>"
  }
}
```

### Hybrid Storage

Use this mode when you want local storage for file operations but need Azure-specific features like SAS token generation:

```json
"FileStorage": {
  "Provider": "Hybrid",
  "Local": {
    "BaseDirectory": "C:\\FileStorage",
    "CreateDirectoryIfNotExists": true
  },
  "Azure": {
    "ConnectionString": "<storage-connection-string>",
    "ShareName": "<share-name>"
  }
}
```

## Usage

### Basic Setup

Register the service in your application:

```csharp
builder.Services.AddFileStorage(builder.Configuration);
```

### File Operations

Inject `IFileStorageService` for standard file operations:

```csharp
public class FileController
{
    private readonly IFileStorageService _fileStorage;

    public FileController(IFileStorageService fileStorage)
    {
        _fileStorage = fileStorage;
    }

    public async Task UploadFile(Stream fileStream, string fileName)
    {
        await _fileStorage.UploadAsync($"documents/{fileName}", fileStream);
    }

    public async Task<Stream> DownloadFile(string fileName)
    {
        return await _fileStorage.DownloadAsync($"documents/{fileName}");
    }

    public async Task DeleteFile(string fileName)
    {
        await _fileStorage.DeleteAsync($"documents/{fileName}");
    }

    public async Task<bool> CheckFileExists(string fileName)
    {
        return await _fileStorage.ExistsAsync($"documents/{fileName}");
    }
}
```

### Azure-Specific Operations (SAS Token Generation)

When using **Azure** or **Hybrid** providers, you can inject `IAzureSpecificOperations` to access Azure-specific features like SAS token generation:

```csharp
public class SecureFileController
{
    private readonly IFileStorageService _fileStorage;
    private readonly IAzureSpecificOperations _azureOperations;

    public SecureFileController(
        IFileStorageService fileStorage, 
        IAzureSpecificOperations azureOperations)
    {
        _fileStorage = fileStorage;
        _azureOperations = azureOperations;
    }

    // Upload using local storage (in Hybrid mode)
    public async Task UploadFile(Stream fileStream, string fileName)
    {
        await _fileStorage.UploadAsync($"documents/{fileName}", fileStream);
    }

    // Generate a read-only SAS token valid for 1 hour
    public async Task<string> GetSecureDownloadLink(string fileName)
    {
        var sasUri = await _azureOperations.GenerateSasTokenAsync(
            $"documents/{fileName}", 
            TimeSpan.FromHours(1), 
            "r" // read-only permission
        );
        return sasUri;
    }

    // Generate a SAS token with custom permissions and expiration
    public async Task<string> GetSecureUploadLink(string fileName)
    {
        var expiresOn = DateTimeOffset.UtcNow.AddHours(2);
        var sasUri = await _azureOperations.GenerateSasTokenAsync(
            $"documents/{fileName}", 
            expiresOn, 
            "rw" // read-write permissions
        );
        return sasUri;
    }
}
```

### SAS Token Permissions

The `permissions` parameter supports the following values:
- `"r"` - Read
- `"w"` - Write
- `"d"` - Delete
- `"c"` - Create
- Combinations like `"rw"`, `"rd"`, `"rwd"`, etc.

## Provider Comparison

| Feature | Local | Azure | Hybrid |
|---------|-------|-------|--------|
| Upload/Download Files | ✅ | ✅ | ✅ (uses Local) |
| Delete Files | ✅ | ✅ | ✅ (uses Local) |
| Check File Exists | ✅ | ✅ | ✅ (uses Local) |
| Generate SAS Tokens | ❌ | ✅ | ✅ (uses Azure) |
| Performance | Fast | Network dependent | Fast for files, Network for SAS |
| Cost | Free (local disk) | Azure storage costs | Azure storage costs |

## Use Cases

- **Local**: Development, testing, small-scale deployments
- **Azure**: Production cloud environments, distributed systems
- **Hybrid**: When you need local performance but require secure external access via SAS tokens
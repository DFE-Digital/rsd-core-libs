# DfE.CoreLibs.FileStorage

This library provides a simple abstraction for file storage. It currently ships with an Azure File Service implementation but can be extended to support other providers.

## Installation

```sh
dotnet add package DfE.CoreLibs.FileStorage
```

## Configuration

Add a `FileStorage` section to your `appsettings.json`:

```json
"FileStorage": {
  "Provider": "Azure",
  "Azure": {
    "ConnectionString": "<storage-connection-string>",
    "ShareName": "<share-name>"
  }
}
```

## Usage

Register the service in your application:

```csharp
builder.Services.AddFileStorage(builder.Configuration);
```

Inject `IFileStorageService` where needed and use `UploadAsync`, `DownloadAsync` and `DeleteAsync` to manage files.

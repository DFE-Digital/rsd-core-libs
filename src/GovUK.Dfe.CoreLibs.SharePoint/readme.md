# GovUK.Dfe.CoreLibs.SharePoint

A Microsoft Graph-based library for accessing SharePoint document libraries from .NET services using app-only (client credentials) authentication. Supports creating folders, listing files, uploading, and downloading.

## Features

- **App-only authentication** via client secret or certificate (`Azure.Identity`)
- **Folder creation** with automatic parent folder creation
- **List files** in a folder (non-recursive)
- **Upload and download** files in document libraries
- **Dependency injection** via `AddSharePointServices`
- **Configuration** via `appsettings.json` or explicit options

## Required Graph permissions

Register an Entra ID application and grant the **application** permission:

- `Sites.ReadWrite.All` (admin consent required)

## Quick start

### 1. Installation

```bash
dotnet add package GovUK.Dfe.CoreLibs.SharePoint
```

### 2. Configuration

Add SharePoint configuration to `appsettings.json`:

```json
{
  "SharePoint": {
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "SiteHostname": "contoso.sharepoint.com",
    "SitePath": "/sites/MySite",
    "LibraryName": "Documents"
  }
}
```

Alternatively, set `SiteId` instead of `SiteHostname`/`SitePath`, and/or `DriveId` instead of `LibraryName`.

Certificate-based auth (instead of `ClientSecret`):

```json
{
  "SharePoint": {
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "CertificatePath": "C:\\certs\\app.pfx",
    "CertificatePassword": "optional-password",
    "SiteId": "contoso.sharepoint.com,guid,guid",
    "DriveId": "b!..."
  }
}
```

### 3. Register services

```csharp
services.AddSharePointServices(configuration);

// Or with explicit options
services.AddSharePointServices(new SharePointOptions
{
    TenantId = "...",
    ClientId = "...",
    ClientSecret = "...",
    SiteHostname = "contoso.sharepoint.com",
    SitePath = "/sites/MySite",
    LibraryName = "Documents"
});
```

### 4. Use the service

```csharp
public class DocumentService(ISharePointService sharePoint)
{
    public async Task SaveReportAsync(Stream content, CancellationToken ct)
    {
        await sharePoint.CreateFolderAsync("reports/2024", ct);
        await sharePoint.UploadFileAsync("reports/2024", "summary.pdf", content, ct);

        var files = await sharePoint.ListFilesAsync("reports/2024", ct);
        await using var download = await sharePoint.DownloadFileAsync("reports/2024", "summary.pdf", ct);
    }
}
```

## API overview

| Method | Description |
|--------|-------------|
| `CreateFolderAsync(folderPath)` | Creates the folder and any missing parents |
| `ListFilesAsync(folderPath)` | Lists files in the folder (not subfolders) |
| `UploadFileAsync(folderPath, fileName, content)` | Uploads or overwrites a file |
| `DownloadFileAsync(folderPath, fileName)` | Downloads file content as a stream |

Paths are relative to the configured document library root. Use an empty string or `/` for the library root when listing, uploading, or downloading.

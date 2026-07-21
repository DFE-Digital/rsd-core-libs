# GovUK.Dfe.CoreLibs.SharePoint

A Microsoft Graph-based library for accessing SharePoint document libraries from .NET services using app-only (client credentials) authentication. Supports creating folders, listing/uploading/downloading/deleting files.

## Features

- **App-only authentication** via client secret or certificate (`Azure.Identity`)
- **Folder creation** with automatic parent folder creation
- **List files** in a folder (non-recursive)
- **Upload and download** files in document libraries
- **Delete** files in document libraries
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
    "SitePath": "/sites/MySite"
  }
}
```

Alternatively, set `SiteId` instead of `SiteHostname`/`SitePath`.

Certificate-based auth (instead of `ClientSecret`):

```json
{
  "SharePoint": {
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "CertificatePath": "C:\\certs\\app.pfx",
    "CertificatePassword": "optional-password",
    "SiteId": "contoso.sharepoint.com,guid,guid"
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
    SitePath = "/sites/MySite"
});
```

### 4. Use the service

Paths start with the **document library name**, then the folder path within that library:

```csharp
public class DocumentService(ISharePointService sharePoint)
{
    public async Task SaveReportAsync(Stream content, CancellationToken ct)
    {
        await sharePoint.CreateFolderAsync("Documents/reports/2024", ct);
        await sharePoint.UploadFileAsync("Documents/reports/2024", "summary.pdf", content, ct);

        var files = await sharePoint.ListFilesAsync("Documents/reports/2024", ct);
        await using var download = await sharePoint.DownloadFileAsync("Documents/reports/2024", "summary.pdf", ct);
        await sharePoint.DeleteFileAsync("Documents/reports/2024", "summary.pdf", ct);
    }
}
```

## API overview

| Method                                           | Description                                |
|--------------------------------------------------|--------------------------------------------|
| `CreateFolderAsync(folderPath)`                  | Creates the folder and any missing parents |
| `ListFilesAsync(folderPath)`                     | Lists files in the folder (not subfolders) |
| `UploadFileAsync(folderPath, fileName, content)` | Uploads or overwrites a file               |
| `DownloadFileAsync(folderPath, fileName)`        | Downloads file content as a stream         |
| `DeleteFileAsync(folderPath, fileName)`          | Deletes a file from the folder             |

Path format: `{LibraryName}/{folder/...}` (e.g. `Documents/reports/2024`). Use just the library name (e.g. `Documents`) for the library root when listing, uploading, downloading, or deleting. `CreateFolderAsync` requires at least one folder under the library.

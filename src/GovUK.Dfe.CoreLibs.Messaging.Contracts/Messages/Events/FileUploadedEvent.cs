namespace GovUK.Dfe.CoreLibs.Messaging.Contracts.Messages.Events;
public record FileUploadedEvent(
    string FileName,
    string? Path,
    bool? IsAzureFileShare,
    string FileUri,
    string ServiceName);
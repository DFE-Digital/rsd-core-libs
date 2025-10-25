namespace GovUK.Dfe.CoreLibs.Messaging.Contracts.Messages.Events;
public record FileUploadedEvent(
    string? FileId,
    string FileName,
    string? Reference,
    string? Path,
    bool? IsAzureFileShare,
    string FileUri,
    string ServiceName);
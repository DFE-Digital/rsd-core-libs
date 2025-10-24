namespace GovUK.Dfe.CoreLibs.Messaging.Contracts.Messages.Events;
public record FileUploadedEvent(
    string FileName,
    bool? IsAzureFileShare,
    string FileUrl,
    string ServiceName);
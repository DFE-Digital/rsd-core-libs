namespace GovUK.Dfe.CoreLibs.Messaging.Contracts.Messages.Events;
public record ScanRequestedEvent(
    string? FileId,
    string FileName,
    string? FileHash,
    string? Reference,
    string? Path,
    bool? IsAzureFileShare,
    string FileUri,
    string ServiceName, 
    Dictionary<string, object>? Metadata);
using GovUK.Dfe.CoreLibs.Messaging.Contracts.Messages.Enums;

namespace GovUK.Dfe.CoreLibs.Messaging.Contracts.Messages.Events;

public record FileScanResultEvent(
    string? FileId,
    string FileName,
    string? Reference,
    string? Path,
    bool? IsAzureFileShare,
    string FileUri,
    string ServiceName,
    string? CorrelationId,
    VirusScanOutcome Outcome,
    string? MalwareName,
    DateTimeOffset ScannedAt,
    string? ScannerVersion,
    string? Message);

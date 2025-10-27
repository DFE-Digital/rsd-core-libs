using GovUK.Dfe.CoreLibs.Messaging.Contracts.Messages.Enums;

namespace GovUK.Dfe.CoreLibs.Messaging.Contracts.Messages.Events;

public record ScanResultEvent(
    string ServiceName,
    string FileUri,
    string FileName,
    string? FileId = null,
    string? Reference = null,
    string? Path = null,
    bool? IsAzureFileShare = null,
    string? CorrelationId = null,
    ScanStatus Status = ScanStatus.Completed,
    VirusScanOutcome? Outcome = null,     // Clean/Infected/Unknown — used only when Completed
    string? MalwareName = null,
    DateTimeOffset? ScannedAt = null,
    string? ScannerVersion = null,
    string? Message = null,               // Optional details or error message
    int? TimeoutSeconds = null,           // Used only when TimedOut
    string? VendorJobId = null            // Used only when Accepted
);
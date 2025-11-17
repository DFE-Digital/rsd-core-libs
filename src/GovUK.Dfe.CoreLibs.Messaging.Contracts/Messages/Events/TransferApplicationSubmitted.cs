namespace GovUK.Dfe.CoreLibs.Messaging.Contracts.Messages.Events;
/// <summary>
/// Event published when a transfer application is submitted in EAT Transfers
/// </summary>
public record TransferApplicationSubmittedEvent(
    string ApplicationId,
    string ApplicationReference,
    string OutgoingTrustUkprn,
    string OutgoingTrustName,
    bool IsFormAMAT,
    DateTime SubmittedOn,
    List<TransferringAcademy> TransferringAcademies,
    Dictionary<string, object>? Metadata);

/// <summary>
/// Represents an academy being transferred
/// </summary>
public record TransferringAcademy(
    string OutgoingAcademyName,
    string OutgoingAcademyUkprn,
    string? IncomingTrustUkprn,
    string IncomingTrustName,
    string? Region,
    string? LocalAuthority,
    Dictionary<string, object>? Metadata);

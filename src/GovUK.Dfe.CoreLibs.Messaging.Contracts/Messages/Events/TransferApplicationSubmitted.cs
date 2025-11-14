namespace GovUK.Dfe.CoreLibs.Messaging.Contracts.Messages.Events;
public record TransferApplicationSubmitted(
    string ApplicationId,
    string ApplicationReference,
    string OutgoingTrustUkprn,
    string OutgoingTrustName,
    bool IsFormAMAT,
    DateTime SubmittedOn,
    List<TransferringAcademy> TransferringAcademies,
    Dictionary<string, object>? Metadata);

public record TransferringAcademy(
    string OutgoingTrustUkprn,
    string? IncomingTrustUkprn,
    string IncomingTrustName,
    string? Region,
    string? LocalAuthority,
    Dictionary<string, object>? Metadata);
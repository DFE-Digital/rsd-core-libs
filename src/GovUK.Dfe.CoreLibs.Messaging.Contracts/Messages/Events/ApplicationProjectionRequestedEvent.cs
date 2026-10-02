using GovUK.Dfe.CoreLibs.Messaging.Contracts.Messages.Enums;

namespace GovUK.Dfe.CoreLibs.Messaging.Contracts.Messages.Events;

/// <summary>
/// Published by FlexForms when an application changes in a way the Prism analytics projection cares about.
/// Consumers must treat it as a notification: fetch the current source state and compare
/// <see cref="SourceRevision"/> against what they have already projected.
/// </summary>
/// <param name="ContractVersion">Version of this contract. Consumers dead-letter versions they do not understand.</param>
/// <param name="TenantId">FlexForms tenant that owns the application.</param>
/// <param name="ApplicationId">Application identifier.</param>
/// <param name="Reason">The transition that triggered the projection request.</param>
/// <param name="SourceRevision">Application-level revision, incremented on every Saved, Submitted and Deleted transition.</param>
/// <param name="ResponseId">Response version the event refers to. Required for Saved and Submitted.</param>
/// <param name="SubmissionId">Deterministic submission identifier. Required for Submitted.</param>
/// <param name="TemplateId">Template identifier, when available.</param>
/// <param name="TemplateVersionId">Template version identifier, when available.</param>
/// <param name="OperationId">Backfill or reconciliation operation identifier. Only set for Resync.</param>
/// <param name="OccurredAt">When the transition happened (UTC).</param>
public record ApplicationProjectionRequestedEvent(
    int ContractVersion,
    Guid TenantId,
    Guid ApplicationId,
    ProjectionReason Reason,
    long SourceRevision,
    Guid? ResponseId,
    Guid? SubmissionId,
    Guid? TemplateId,
    Guid? TemplateVersionId,
    Guid? OperationId,
    DateTime OccurredAt)
{
    public const int CurrentContractVersion = 1;
}

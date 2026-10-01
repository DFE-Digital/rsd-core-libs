using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;

namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;

/// <summary>A tenant Prism should project. Returned by the internal Prism tenants endpoint.</summary>
public sealed record PrismTenantDto(
    Guid TenantId,
    string TenantName);

/// <summary>
/// Current source state of an application for the Prism projector. Deleted applications are returned
/// (with <see cref="IsDeleted"/> set) so deletions stay discoverable.
/// </summary>
/// <param name="SourceRevision">Application-level revision; compare against the projected revision.</param>
/// <param name="ResponseId">Latest response version, if any.</param>
/// <param name="ResponseRevision">The latest response's CreatedAtRevision.</param>
/// <param name="ResponseBody">The latest response body (JSON).</param>
/// <param name="SubmittedRevision">SourceRevision of the submit transition, when submitted.</param>
/// <param name="SubmissionId">Deterministic submission id, when submitted.</param>
/// <param name="SubmittedResponseId">The response that was current at submission, when submitted.</param>
public sealed record PrismApplicationStateDto(
    Guid ApplicationId,
    string ApplicationReference,
    long SourceRevision,
    ApplicationStatus? Status,
    bool IsDeleted,
    DateTime? DeletedOn,
    Guid TemplateId,
    Guid TemplateVersionId,
    DateTime CreatedOn,
    DateTime? LastModifiedOn,
    Guid? ResponseId,
    long? ResponseRevision,
    string? ResponseBody,
    long? SubmittedRevision,
    Guid? SubmissionId,
    Guid? SubmittedResponseId);

/// <summary>A single, immutable response version.</summary>
public sealed record PrismResponseDto(
    Guid ResponseId,
    Guid ApplicationId,
    long CreatedAtRevision,
    DateTime CreatedOn,
    string ResponseBody);

/// <summary>An immutable template version and its JSON schema.</summary>
public sealed record PrismTemplateVersionDto(
    Guid TemplateVersionId,
    Guid TemplateId,
    string VersionNumber,
    string JsonSchema,
    DateTime CreatedOn);

/// <summary>Lightweight application row used by backfill and reconciliation.</summary>
/// <param name="LastChangedOn">LastModifiedOn, or CreatedOn when the application was never modified.</param>
public sealed record PrismApplicationSummaryDto(
    Guid ApplicationId,
    long SourceRevision,
    ApplicationStatus? Status,
    bool IsDeleted,
    DateTime LastChangedOn);

/// <summary>
/// A page of applications ordered by creation time (oldest first), so new applications only ever append.
/// </summary>
public sealed record PrismApplicationPageDto(
    IReadOnlyList<PrismApplicationSummaryDto> Items,
    int Page,
    int PageSize,
    bool HasMore);

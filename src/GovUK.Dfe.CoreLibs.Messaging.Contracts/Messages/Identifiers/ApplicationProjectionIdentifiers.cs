using System.Globalization;
using GovUK.Dfe.CoreLibs.Messaging.Contracts.Messages.Enums;

namespace GovUK.Dfe.CoreLibs.Messaging.Contracts.Messages.Identifiers;

/// <summary>
/// Identifiers shared by the FlexForms publisher and the Prism projector for
/// <see cref="Events.ApplicationProjectionRequestedEvent"/> and <see cref="Events.TemplateVersionPublishedEvent"/>.
/// Both sides must derive them the same way.
/// </summary>
public static class ApplicationProjectionIdentifiers
{
    /// <summary>UUIDv5 namespace for all Prism identifiers. Never change this value.</summary>
    public static readonly Guid Namespace = new("6f1c2a4e-8d3b-4f57-9a0e-2b7c5d9e1f30");

    /// <summary>Service Bus session id: every message for one application is processed in order.</summary>
    public static string SessionId(Guid tenantId, Guid applicationId)
        => $"{tenantId:D}:{applicationId:D}";

    /// <summary>
    /// Deterministic message id, used for Service Bus duplicate detection. Resync messages include the
    /// operation id so a later backfill of the same revision is not discarded as a duplicate.
    /// </summary>
    public static Guid MessageId(
        Guid tenantId,
        Guid applicationId,
        long sourceRevision,
        ProjectionReason reason,
        Guid? operationId = null)
    {
        var name = string.Create(
            CultureInfo.InvariantCulture,
            $"prism:{tenantId:D}:{applicationId:D}:{sourceRevision}:{reason}");

        if (reason == ProjectionReason.Resync)
        {
            if (operationId is null || operationId == Guid.Empty)
                throw new ArgumentException("Resync message ids require an operation id.", nameof(operationId));

            name += $":{operationId:D}";
        }

        return DeterministicGuid.Create(Namespace, name);
    }

    /// <summary>
    /// Service Bus session id for <see cref="Events.TemplateVersionPublishedEvent"/>: the versions of one template are
    /// processed in order. Cannot collide with <see cref="SessionId"/>, whose second part is a bare GUID.
    /// </summary>
    public static string TemplateSessionId(Guid tenantId, Guid templateId)
        => $"{tenantId:D}:template:{templateId:D}";

    /// <summary>Deterministic message id for <see cref="Events.TemplateVersionPublishedEvent"/>, used for duplicate detection.</summary>
    public static Guid TemplateVersionMessageId(Guid tenantId, Guid templateVersionId)
        => DeterministicGuid.Create(
            Namespace,
            string.Create(CultureInfo.InvariantCulture, $"prism-template-version:{tenantId:D}:{templateVersionId:D}"));

    /// <summary>Submission id for the single submission of an application, made at <paramref name="submittedRevision"/>.</summary>
    public static Guid SubmissionId(Guid applicationId, long submittedRevision)
        => DeterministicGuid.Create(
            Namespace,
            string.Create(CultureInfo.InvariantCulture, $"submission:{applicationId:D}:{submittedRevision}"));
}

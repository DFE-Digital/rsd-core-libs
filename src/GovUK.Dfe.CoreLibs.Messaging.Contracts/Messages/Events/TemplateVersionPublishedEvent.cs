namespace GovUK.Dfe.CoreLibs.Messaging.Contracts.Messages.Events;

/// <summary>
/// Published by FlexForms when a new template version is created. Template versions are immutable, so every
/// change to a form (fields added or removed, labels, types, pages) arrives as a new version. Prism uses it to
/// catalogue the version's fields straight away, before any application uses it.
/// Consumers must treat it as a notification and read the version itself from the source.
/// </summary>
/// <param name="ContractVersion">Version of this contract. Consumers dead-letter versions they do not understand.</param>
/// <param name="TenantId">FlexForms tenant that owns the template.</param>
/// <param name="TemplateId">Template identifier.</param>
/// <param name="TemplateVersionId">The new template version.</param>
/// <param name="VersionNumber">The version number given by the template author.</param>
/// <param name="CreatedAt">When the version was created (UTC).</param>
public record TemplateVersionPublishedEvent(
    int ContractVersion,
    Guid TenantId,
    Guid TemplateId,
    Guid TemplateVersionId,
    string VersionNumber,
    DateTime CreatedAt)
{
    public const int CurrentContractVersion = 1;
}

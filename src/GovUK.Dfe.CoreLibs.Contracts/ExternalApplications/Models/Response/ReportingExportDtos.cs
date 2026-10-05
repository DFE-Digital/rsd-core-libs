using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;

namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;

/// <summary>A field of a template and whether its answers are exported to reporting.</summary>
/// <param name="ParentFieldId">The repeating section the field belongs to; empty for top-level fields.</param>
/// <param name="TemplateVersionNumber">The most recent template version that has the field.</param>
public sealed record ReportingExportFieldDto(
    string ParentFieldId,
    string FieldId,
    string? Label,
    string? DataType,
    bool IsCollection,
    string? TaskName,
    string? PageTitle,
    string? TemplateVersionNumber,
    ReportingExportStatus Status,
    string? Reason,
    string? DecidedBy,
    DateTime? DecidedAt);

/// <summary>Every field of a template with its export status, and the default for undecided fields.</summary>
public sealed record ReportingExportPolicyDto(
    Guid TemplateId,
    int PolicyVersion,
    ReportingExportMode DefaultMode,
    ReportingExportDefaultSource DefaultSource,
    IReadOnlyList<ReportingExportFieldDto> Fields);

/// <summary>
/// The default for undecided fields, for the tenant (<see cref="TemplateId"/> null) or one template.
/// <see cref="Mode"/> is what was set at this level; <see cref="EffectiveMode"/> and <see cref="Source"/> are what
/// applies once inheritance is resolved.
/// </summary>
public sealed record ReportingExportDefaultDto(
    Guid? TemplateId,
    ReportingExportModeSetting Mode,
    ReportingExportMode EffectiveMode,
    ReportingExportDefaultSource Source,
    string? Reason,
    string? DecidedBy,
    DateTime? DecidedAt);

/// <summary>The outcome of a change.</summary>
/// <param name="Warnings">For example, a field allowed inside a repeating section that is not exported.</param>
/// <param name="RefreshId">The refresh that re-exports the tenant's applications; null when nothing changed.</param>
public sealed record ReportingExportChangeResultDto(
    ReportingExportChangeStatus Status,
    int PolicyVersion,
    int Changed,
    IReadOnlyList<string> Warnings,
    Guid? RefreshId);

/// <summary>Progress of a refresh started by a change.</summary>
/// <param name="ApplicationsQueued">Applications queued to be re-exported so far.</param>
public sealed record ReportingExportRefreshDto(
    Guid RefreshId,
    ReportingExportRefreshStatus Status,
    int ApplicationsScanned,
    int ApplicationsQueued,
    string? Error,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt);

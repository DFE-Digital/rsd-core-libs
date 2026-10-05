using System.Text.Json.Serialization;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;

namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request;

/// <summary>An export decision for one field of a template.</summary>
public class ReportingExportDecisionRequest
{
    /// <summary>The repeating section the field belongs to; null or empty for top-level fields.</summary>
    [JsonPropertyName("parentFieldId")]
    public string? ParentFieldId { get; set; }

    [JsonPropertyName("fieldId")]
    public string FieldId { get; set; } = string.Empty;

    [JsonPropertyName("decision")]
    public ReportingExportDecision Decision { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

/// <summary>Export decisions to apply to a template. Fields left out keep their current decision.</summary>
public class UpdateReportingExportDecisionsRequest
{
    [JsonPropertyName("decisions")]
    public List<ReportingExportDecisionRequest> Decisions { get; set; } = [];
}

/// <summary>Sets the default for undecided fields, for the tenant or one template.</summary>
public class UpdateReportingExportDefaultRequest
{
    [JsonPropertyName("mode")]
    public ReportingExportModeSetting Mode { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

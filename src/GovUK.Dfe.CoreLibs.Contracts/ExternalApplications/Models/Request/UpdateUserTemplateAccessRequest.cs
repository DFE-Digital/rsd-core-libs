using System.Text.Json.Serialization;

namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request;

/// <summary>
/// Replaces the user's form (template) access within the current tenant.
/// </summary>
public sealed class UpdateUserTemplateAccessRequest
{
    [JsonPropertyName("templateIds")]
    public IReadOnlyCollection<Guid> TemplateIds { get; set; } = [];
}

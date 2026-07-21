using System.Text.Json.Serialization;

namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;

/// <summary>
/// A user visible within the current tenant, including form (template) access.
/// </summary>
public sealed class TenantUserDto
{
    [JsonPropertyName("userId")]
    public Guid UserId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("templates")]
    public IReadOnlyList<TenantUserTemplateDto> Templates { get; set; } = [];
}

/// <summary>
/// A template the user can access within the tenant.
/// </summary>
public sealed class TenantUserTemplateDto
{
    [JsonPropertyName("templateId")]
    public Guid TemplateId { get; set; }

    [JsonPropertyName("templateName")]
    public string TemplateName { get; set; } = string.Empty;

    [JsonPropertyName("isLive")]
    public bool IsLive { get; set; }
}

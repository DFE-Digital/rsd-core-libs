using System.Text.Json.Serialization;

namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request;

/// <summary>
/// Renames a custom tenant role.
/// </summary>
public class RenameTenantRoleRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;
}

using System.Text.Json.Serialization;

namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request;

/// <summary>
/// Creates a custom tenant role (non-system).
/// </summary>
public class CreateTenantRoleRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;
}

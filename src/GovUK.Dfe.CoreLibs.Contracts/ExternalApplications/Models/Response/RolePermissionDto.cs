using System.Text.Json.Serialization;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;

namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;

/// <summary>
/// A permission grant attached to a role.
/// </summary>
public class RolePermissionDto
{
    [JsonPropertyName("rolePermissionId")]
    public Guid RolePermissionId { get; set; }

    [JsonPropertyName("resourceType")]
    public ResourceType ResourceType { get; set; }

    [JsonPropertyName("resourceKey")]
    public string ResourceKey { get; set; } = null!;

    [JsonPropertyName("accessType")]
    public AccessType AccessType { get; set; }
}

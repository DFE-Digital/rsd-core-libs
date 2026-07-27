using System.Text.Json.Serialization;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;

namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request;

/// <summary>
/// A single permission grant to attach to a role.
/// </summary>
public class RolePermissionGrantDto
{
    [JsonPropertyName("resourceType")]
    public ResourceType ResourceType { get; set; }

    [JsonPropertyName("resourceKey")]
    public string ResourceKey { get; set; } = null!;

    [JsonPropertyName("accessType")]
    public AccessType AccessType { get; set; }
}

/// <summary>
/// Replaces the full set of permissions on a tenant role.
/// </summary>
public class SetRolePermissionsRequest
{
    [JsonPropertyName("permissions")]
    public IReadOnlyCollection<RolePermissionGrantDto> Permissions { get; set; }
        = Array.Empty<RolePermissionGrantDto>();
}

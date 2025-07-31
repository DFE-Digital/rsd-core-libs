using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using System.Text.Json.Serialization;

namespace DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;

public class UserPermissionDto
{
    [JsonPropertyName("applicationId")]
    public Guid? ApplicationId { get; set; } = null;
    
    [JsonPropertyName("resourceKey")]
    public string ResourceKey { get; set; } = string.Empty;
    
    [JsonPropertyName("resourceType")]
    public required ResourceType ResourceType { get; set; }
    
    [JsonPropertyName("accessType")]
    public required AccessType AccessType { get; set; }
}
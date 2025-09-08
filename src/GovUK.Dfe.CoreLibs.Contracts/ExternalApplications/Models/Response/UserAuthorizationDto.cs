using System.Text.Json.Serialization;

namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;

public class UserAuthorizationDto
{
    [JsonPropertyName("permissions")]
    public required IEnumerable<UserPermissionDto> Permissions { get; set; }
    
    [JsonPropertyName("roles")]
    public required IEnumerable<string> Roles { get; set; }
}

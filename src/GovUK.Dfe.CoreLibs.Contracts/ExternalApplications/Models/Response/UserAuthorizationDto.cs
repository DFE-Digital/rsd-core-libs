using System.Text.Json.Serialization;

namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;

public class UserAuthorizationDto
{
    [JsonPropertyName("permissions")]
    public required IEnumerable<UserPermissionDto> Permissions { get; set; }
    
    [JsonPropertyName("roles")]
    public required IEnumerable<string> Roles { get; set; }

    /// <summary>
    /// Application resource keys for which the user has a contributor-invite scope
    /// (<c>ContributorInvite:{key}:Scope</c>). Clients that rebuild permission claims from this DTO
    /// must emit those markers so tenant-wide ApplicationFiles Any does not override unticked file boxes.
    /// </summary>
    [JsonPropertyName("inviteScopedApplicationKeys")]
    public IEnumerable<string> InviteScopedApplicationKeys { get; set; } = Array.Empty<string>();
}

using System.Text.Json.Serialization;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;

namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request;

/// <summary>
/// Request body for replacing a user's direct (user-level) permission grants.
/// Does not affect permissions inherited from the user's role.
/// </summary>
public class SetUserPermissionsRequest
{
    /// <summary>
    /// The complete set of user-level permission grants. Replaces all existing user-owned grants.
    /// </summary>
    [JsonPropertyName("permissions")]
    public IReadOnlyCollection<RolePermissionGrantDto> Permissions { get; set; }
        = Array.Empty<RolePermissionGrantDto>();
}

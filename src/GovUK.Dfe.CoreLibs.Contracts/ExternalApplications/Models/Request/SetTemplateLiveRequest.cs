using System.Text.Json.Serialization;

namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request;

/// <summary>
/// Request to set whether a template is live for end users in the current tenant.
/// </summary>
public class SetTemplateLiveRequest
{
    /// <summary>
    /// When <c>true</c>, the template is available to end users who have permission.
    /// </summary>
    [JsonPropertyName("isLive")]
    public bool IsLive { get; set; }
}

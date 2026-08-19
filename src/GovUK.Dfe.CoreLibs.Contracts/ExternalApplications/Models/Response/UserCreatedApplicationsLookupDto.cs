using System.Text.Json.Serialization;

namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;

/// <summary>
/// A user looked up by email, the applications they created, and people they invited to those applications.
/// </summary>
public sealed class UserCreatedApplicationsLookupDto
{
    [JsonPropertyName("userId")]
    public Guid UserId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("applications")]
    public IReadOnlyList<CreatedApplicationWithInviteesDto> Applications { get; set; } = [];
}

/// <summary>
/// An application created by the looked-up user, with invitees they granted access to.
/// </summary>
public sealed class CreatedApplicationWithInviteesDto
{
    [JsonPropertyName("applicationId")]
    public Guid ApplicationId { get; set; }

    [JsonPropertyName("applicationReference")]
    public string ApplicationReference { get; set; } = string.Empty;

    [JsonPropertyName("templateName")]
    public string TemplateName { get; set; } = string.Empty;

    [JsonPropertyName("dateCreated")]
    public DateTime DateCreated { get; set; }

    [JsonPropertyName("invitees")]
    public IReadOnlyList<ApplicationInviteeDto> Invitees { get; set; } = [];
}

/// <summary>
/// Someone invited onto an application. Identified by user id and email.
/// </summary>
public sealed class ApplicationInviteeDto
{
    [JsonPropertyName("userId")]
    public Guid UserId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("grantedOn")]
    public DateTime GrantedOn { get; set; }
}

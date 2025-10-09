namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request;
public class RegisterUserRequest
{
    public string AccessToken { get; set; } = null!;
    public Guid TemplateId { get; set; }
}

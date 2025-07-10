namespace DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;

public class UserAuthorizationDto
{
    public required IEnumerable<UserPermissionDto> Permissions { get; set; }
    public required IEnumerable<string> Roles { get; set; }
}
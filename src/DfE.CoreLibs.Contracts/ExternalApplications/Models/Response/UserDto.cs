namespace DfE.CoreLibs.Contracts.ExternalApplications.Models.Response
{
    public class UserDto
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public Guid RoleId { get; set; }
        public UserAuthorizationDto? Authorization { get; set; }
    }
}
namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response
{
    public sealed record TenantDetailDto(Guid Id, string Name, string[] FrontendOrigins);
}

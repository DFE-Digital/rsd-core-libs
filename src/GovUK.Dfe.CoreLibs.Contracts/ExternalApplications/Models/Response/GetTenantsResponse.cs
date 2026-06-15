namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response
{
    public sealed record GetTenantsResponse(
        string Source,
        int TenantCount,
        IReadOnlyCollection<TenantDetailDto> Tenants);


}

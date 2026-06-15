namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response
{
    public sealed record RefreshTenantConfigurationResponse(
        string Message,
        int TenantCount,
        IReadOnlyCollection<TenantSummaryDto> Tenants);


}

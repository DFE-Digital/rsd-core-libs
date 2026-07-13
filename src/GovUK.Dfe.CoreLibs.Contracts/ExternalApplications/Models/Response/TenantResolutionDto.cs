namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response
{
    public sealed record TenantResolutionDto(
    Guid TenantId,
    string TenantName,
    string Hostname);
}

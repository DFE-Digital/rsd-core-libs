namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response
{
    /// <summary>
    /// Raw TenantConfig settings rows for a tenant (for SuperAdmin editing).
    /// </summary>
    public sealed record GetTenantSettingsResponse(
        Guid TenantId,
        string TenantName,
        IReadOnlyCollection<TenantSettingDto> Settings);
}

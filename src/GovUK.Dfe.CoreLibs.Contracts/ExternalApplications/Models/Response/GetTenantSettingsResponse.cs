namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response
{
    /// <summary>
    /// TenantConfig settings rows for a tenant. Secret-bearing leaves are redacted.
    /// </summary>
    public sealed record GetTenantSettingsResponse(
        Guid TenantId,
        string TenantName,
        IReadOnlyCollection<TenantSettingDto> Settings);
}

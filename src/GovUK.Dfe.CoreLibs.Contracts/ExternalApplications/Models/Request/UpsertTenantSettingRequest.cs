namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request
{
    /// <summary>
    /// Request body for upserting a tenant setting section.
    /// </summary>
    public sealed record UpsertTenantSettingRequest(
        string Category,
        string Target,
        string SettingsJson,
        bool IsSecret);
}

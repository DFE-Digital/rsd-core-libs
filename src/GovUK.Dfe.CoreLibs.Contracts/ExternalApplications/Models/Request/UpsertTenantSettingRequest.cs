namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request
{
    /// <summary>
    /// Request body for upserting a tenant setting section.
    /// <see cref="SettingsJson"/> must be UTF-8 JSON encoded as Base64
    /// (same WAF-safe transport pattern as template JsonSchema).
    /// </summary>
    public sealed record UpsertTenantSettingRequest(
        string Category,
        string Target,
        string SettingsJson,
        bool IsSecret);
}

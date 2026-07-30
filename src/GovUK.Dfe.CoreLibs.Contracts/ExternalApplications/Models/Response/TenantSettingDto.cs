namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response
{
    /// <summary>
    /// A single TenantConfig settings row (category JSON blob), with secrets decrypted for authorised callers.
    /// </summary>
    public sealed record TenantSettingDto(
        Guid SettingId,
        string Category,
        string Target,
        string SettingsJson,
        bool IsSecret,
        DateTime UpdatedAtUtc);
}

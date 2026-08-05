namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request;

/// <summary>
/// Dry-run validation of a tenant setting change (Base64 SettingsJson, same as upsert).
/// </summary>
public sealed record ValidateTenantSettingRequest(
    string Category,
    string Target,
    string SettingsJson,
    bool IsSecret);

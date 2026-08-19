namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;

/// <summary>
/// Result of validating a tenant setting before save (dry-run).
/// </summary>
public sealed record ValidateTenantSettingResponse(
    bool IsValid,
    IReadOnlyList<string> Errors,
    string? DiffSummary,
    string? CurrentSettingsJson,
    string ProposedSettingsJson,
    bool SettingExists);

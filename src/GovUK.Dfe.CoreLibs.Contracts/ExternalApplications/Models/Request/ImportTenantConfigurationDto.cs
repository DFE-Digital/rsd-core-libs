namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request;

public sealed record TenantSettingImportItemDto(
    string Category,
    string Target,
    string SettingsJson,
    bool IsSecret);

public sealed record ImportTenantConfigurationDto(
    IReadOnlyList<TenantSettingImportItemDto> Settings,
    bool SkipSecretPlaceholders = true);

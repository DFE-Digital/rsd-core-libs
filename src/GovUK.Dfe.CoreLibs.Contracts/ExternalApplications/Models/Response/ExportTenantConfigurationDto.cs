namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;

public sealed record TenantSettingExportDto(
    string Category,
    string Target,
    string SettingsJson,
    bool IsSecret,
    bool SecretRedacted);

public sealed record ExportTenantConfigurationDto(
    Guid TenantId,
    string TenantName,
    DateTimeOffset ExportedAtUtc,
    IReadOnlyList<TenantSettingExportDto> Settings);

namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;

public sealed record DeleteTenantSettingResponse(
    Guid TenantId,
    string Category,
    string Target,
    string Message);

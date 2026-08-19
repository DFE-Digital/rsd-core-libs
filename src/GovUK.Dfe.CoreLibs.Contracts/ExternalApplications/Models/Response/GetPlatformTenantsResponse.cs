namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;

public sealed record PlatformTenantSummaryDto(
    Guid TenantId,
    string TenantName,
    bool IsActive,
    IReadOnlyList<string> Hostnames,
    IReadOnlyList<string> FrontendOrigins,
    string? InteractiveAuthScheme);

public sealed record GetPlatformTenantsResponse(
    string Source,
    int TenantCount,
    DateTimeOffset? LastCatalogueRefreshUtc,
    IReadOnlyList<PlatformTenantSummaryDto> Tenants);

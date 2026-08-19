namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;

public sealed record TenantEffectiveConfigurationDto(
    Guid TenantId,
    string TenantName,
    string ConfigSource,
    DateTimeOffset? LastCatalogueRefreshUtc,
    int ActiveTenantCount,
    string? InteractiveAuthScheme,
    bool TestAuthenticationEnabled,
    bool EntraSsoEnabled,
    bool DfESignInConfigured,
    int RegisteredAuthProviderCount,
    IReadOnlyList<string> Hostnames,
    IReadOnlyList<string> FrontendOrigins);

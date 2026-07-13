namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response
{
    public sealed record TenantConfigurationDto(
        Guid TenantId,
        string TenantName,
        string Target,
        DateTime LoadedAtUtc,
        IReadOnlyDictionary<string, string?> Configuration);
}

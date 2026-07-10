namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;

/// <summary>
/// Global host configuration returned by the platform host-config endpoint.
/// </summary>
public sealed record HostConfigurationDto(
    string Target,
    DateTime LoadedAtUtc,
    IReadOnlyDictionary<string, string?> Configuration);

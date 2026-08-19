namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;

public sealed record ImportTenantConfigurationResultDto(
    int AppliedCount,
    int SkippedCount,
    IReadOnlyList<string> Messages);

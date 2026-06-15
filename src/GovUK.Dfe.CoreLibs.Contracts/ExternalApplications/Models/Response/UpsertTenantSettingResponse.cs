namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response
{
    public sealed record UpsertTenantSettingResponse(
        Guid SettingId,
        bool WasCreated,
        string Category,
        string Target,
        string Message);


}

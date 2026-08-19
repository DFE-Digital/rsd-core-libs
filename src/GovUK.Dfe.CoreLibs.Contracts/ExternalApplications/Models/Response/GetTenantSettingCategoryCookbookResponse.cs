namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;

public sealed record TenantSettingCategoryCookbookEntryDto(
    string Category,
    string Description,
    IReadOnlyList<string> TypicalTargets,
    bool IsSecretCategory,
    bool RequiresObjectRoot,
    string ExampleJson,
    IReadOnlyList<string> Notes);

public sealed record GetTenantSettingCategoryCookbookResponse(
    IReadOnlyList<TenantSettingCategoryCookbookEntryDto> Categories);

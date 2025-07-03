namespace DfE.CoreLibs.Contracts.ExternalApplications.Models.Response
{
    public sealed record ApplicationResponseDto(
        Guid ResponseId,
        string ApplicationReference,
        Guid ApplicationId,
        string ResponseBody,
        DateTime CreatedOn,
        Guid CreatedBy);
}
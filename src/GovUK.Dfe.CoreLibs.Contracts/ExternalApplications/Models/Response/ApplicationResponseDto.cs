using System.Text.Json.Serialization;

namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response
{
    public sealed record ApplicationResponseDto(
        [property: JsonPropertyName("responseId")] Guid ResponseId,
        [property: JsonPropertyName("applicationReference")] string ApplicationReference,
        [property: JsonPropertyName("applicationId")] Guid ApplicationId,
        [property: JsonPropertyName("responseBody")] string ResponseBody,
        [property: JsonPropertyName("createdOn")] DateTime CreatedOn,
        [property: JsonPropertyName("createdBy")] Guid CreatedBy);
}

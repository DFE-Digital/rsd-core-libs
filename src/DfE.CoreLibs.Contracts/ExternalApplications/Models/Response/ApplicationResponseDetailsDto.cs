using System.Text.Json.Serialization;

namespace DfE.CoreLibs.Contracts.ExternalApplications.Models.Response
{
    public sealed record ApplicationResponseDetailsDto
    {
        [JsonPropertyName("responseId")]
        public Guid ResponseId { get; init; }
        
        [JsonPropertyName("responseBody")]
        public string ResponseBody { get; init; } = null!;
        
        [JsonPropertyName("createdOn")]
        public DateTime CreatedOn { get; init; }
        
        [JsonPropertyName("createdBy")]
        public Guid CreatedBy { get; init; }
        
        [JsonPropertyName("lastModifiedOn")]
        public DateTime? LastModifiedOn { get; init; }
        
        [JsonPropertyName("lastModifiedBy")]
        public Guid? LastModifiedBy { get; init; }
    }
}

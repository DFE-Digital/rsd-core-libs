using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using System.Text.Json.Serialization;

namespace DfE.CoreLibs.Contracts.ExternalApplications.Models.Response
{
    public sealed record ApplicationDto
    {
        [JsonPropertyName("applicationId")]
        public Guid ApplicationId { get; set; }
        
        [JsonPropertyName("applicationReference")]
        public string ApplicationReference { get; set; } = string.Empty;
        
        [JsonPropertyName("templateVersionId")]
        public Guid TemplateVersionId { get; set; }
        
        [JsonPropertyName("templateName")]
        public string TemplateName { get; set; } = string.Empty;
        
        [JsonPropertyName("status")]
        public ApplicationStatus? Status { get; set; } = null;
        
        [JsonPropertyName("dateCreated")]
        public DateTime DateCreated { get; set; }
        
        [JsonPropertyName("dateSubmitted")]
        public DateTime? DateSubmitted { get; set; }
        
        [JsonPropertyName("createdBy")]
        public UserDto? CreatedBy { get; init; } = null;
        
        [JsonPropertyName("latestResponse")]
        public ApplicationResponseDetailsDto? LatestResponse { get; init; } = null;
        
        [JsonPropertyName("templateSchema")]
        public TemplateSchemaDto? TemplateSchema { get; set; }

    }
}

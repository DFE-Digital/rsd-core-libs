using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using System.Text.Json.Serialization;

namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response
{

    public class UploadDto
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
        
        [JsonPropertyName("applicationId")]
        public Guid ApplicationId { get; set; }
        
        [JsonPropertyName("uploadedBy")]
        public Guid UploadedBy { get; set; }
        
        [JsonPropertyName("uploadedByUser")]
        public UserDto? UploadedByUser { get; set; } = null;
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [JsonPropertyName("originalFileName")]
        public string OriginalFileName { get; set; } = null!;
        
        [JsonPropertyName("fileName")]
        public string FileName { get; set; } = null!;

        [JsonPropertyName("fileSize")]
        public long FileSize { get; set; }

        [JsonPropertyName("uploadedOn")]
        public DateTime UploadedOn { get; set; }

        [JsonPropertyName("validationStatus")]
        public FileValidationStatus ValidationStatus { get; set; } = FileValidationStatus.NotRequired;

        [JsonPropertyName("validationMessage")]
        public string? ValidationMessage { get; set; }

        [JsonPropertyName("validatedOn")]
        public DateTime? ValidatedOn { get; set; }
    }
}

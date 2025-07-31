using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using System.Text.Json.Serialization;

namespace DfE.CoreLibs.Contracts.ExternalApplications.Models.Response
{
    public class TemplatePermissionDto
    {
        [JsonPropertyName("templatePermissionId")]
        public Guid? TemplatePermissionId { get; set; } = null;
        
        [JsonPropertyName("templateId")]
        public required Guid TemplateId { get; set; }
        
        [JsonPropertyName("userId")]
        public required Guid UserId { get; set; }
        
        [JsonPropertyName("accessType")]
        public required AccessType AccessType { get; set; }
    }
}

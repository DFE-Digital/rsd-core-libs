using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using System;
using System.Text.Json.Serialization;

namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response
{
    public sealed class CustomApplicationStatusDto
    {

        [JsonPropertyName("customApplicationStatusId")]
        public Guid? CustomApplicationStatusId { get; set; }
        [JsonPropertyName("templateId")]
        public Guid TemplateId { get; set; }
        [JsonPropertyName("applicationStatus")]
        public ApplicationStatus ApplicationStatus { get; set; }
        [JsonPropertyName("label")]
        public string? Label { get; set; }
        [JsonPropertyName("createdOn")]
        public DateTime CreatedOn { get; set; }
        [JsonPropertyName("createdBy")]
        public Guid CreatedBy { get; set; }

    }
}
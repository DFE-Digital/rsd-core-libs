using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using System.Text.Json.Serialization;

namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request
{
    public sealed class CustomApplicationStatusRequest
    {
        [JsonPropertyName("applicationStatus")]
        public ApplicationStatus ApplicationStatus { get; set; }
        [JsonPropertyName("label")]
        public string? Label { get; set; }
    }
}
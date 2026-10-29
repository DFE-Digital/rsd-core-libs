using System.Text.Json.Serialization;

namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response
{
    public sealed class ChangeApplicationOwnerResponse
    {
        [JsonPropertyName("applicationId")]
        public Guid ApplicationId { get; set; }

        [JsonPropertyName("templateReadGranted")]
        public bool TemplateReadGranted { get; set; }
    }
}

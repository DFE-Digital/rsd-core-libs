using System.Text.Json.Serialization;

namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response
{
    public class DownloadFileResult
    {
        [JsonPropertyName("fileStream")]
        public Stream FileStream { get; set; } = null!;
        
        [JsonPropertyName("fileName")]
        public string FileName { get; set; } = null!;
        
        [JsonPropertyName("contentType")]
        public string ContentType { get; set; } = null!;
    }
}

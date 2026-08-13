using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using System.Text.Json.Serialization;

namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response
{
    public sealed class FileValidationGateDto
    {
        [JsonPropertyName("mode")]
        public FileValidationMode Mode { get; set; }

        [JsonPropertyName("canSubmit")]
        public bool CanSubmit { get; set; }

        [JsonPropertyName("blockingFiles")]
        public IReadOnlyList<FileValidationBlockDto> BlockingFiles { get; set; } = [];
    }

    public sealed class FileValidationBlockDto
    {
        [JsonPropertyName("fileId")]
        public Guid FileId { get; set; }

        [JsonPropertyName("originalFileName")]
        public string OriginalFileName { get; set; } = string.Empty;

        [JsonPropertyName("validationStatus")]
        public FileValidationStatus ValidationStatus { get; set; }

        [JsonPropertyName("validationMessage")]
        public string? ValidationMessage { get; set; }
    }
}

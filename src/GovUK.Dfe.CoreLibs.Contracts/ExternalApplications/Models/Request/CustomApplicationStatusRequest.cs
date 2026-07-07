using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request
{
    /// <summary>
    /// Request to create or update a custom application status label for a template.
    /// </summary>
    public sealed class CustomApplicationStatusRequest
    {
        /// <summary>
        /// The application status to customise.
        /// </summary>
        [Required]
        [JsonPropertyName("applicationStatus")]
        public ApplicationStatus? ApplicationStatus { get; set; }

        /// <summary>
        /// The custom label to display for the application status.
        /// </summary>
        [Required]
        [JsonPropertyName("label")]
        public string Label { get; set; } = null!;
    }
}

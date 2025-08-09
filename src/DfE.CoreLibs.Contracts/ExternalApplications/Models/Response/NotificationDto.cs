using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using System.Text.Json.Serialization;

namespace DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;

public class NotificationDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;
    [JsonPropertyName("message")]
    public string Message { get; set; } = null!;
    [JsonPropertyName("type")]
    public NotificationType Type { get; set; }
    [JsonPropertyName("category")]
    public string? Category { get; set; }
    [JsonPropertyName("context")]
    public string? Context { get; set; }
    [JsonPropertyName("isRead")]
    public bool IsRead { get; set; }
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
    [JsonPropertyName("autoDismiss")]
    public bool AutoDismiss { get; set; }
    [JsonPropertyName("autoDismissSeconds")]
    public int AutoDismissSeconds { get; set; }
    [JsonPropertyName("userId")]
    public string? UserId { get; set; }
    [JsonPropertyName("actionUrl")]
    public string? ActionUrl { get; set; }
    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
    [JsonPropertyName("priority")]
    public NotificationPriority Priority { get; set; }
}

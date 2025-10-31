using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using System.ComponentModel.DataAnnotations;

namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request;

public class AddNotificationRequest
{
    [Required]
    [MaxLength(1000)]
    public string Message { get; set; } = null!;

    [Required]
    public NotificationType Type { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(100)]
    public string? Context { get; set; }

    public bool? AutoDismiss { get; set; }

    public int? AutoDismissSeconds { get; set; }

    [MaxLength(500)]
    public string? ActionUrl { get; set; }

    public Dictionary<string, object>? Metadata { get; set; }

    public NotificationPriority? Priority { get; set; }

    public bool? ReplaceExistingContext { get; set; }

    public Guid? UserId { get; set; }
}

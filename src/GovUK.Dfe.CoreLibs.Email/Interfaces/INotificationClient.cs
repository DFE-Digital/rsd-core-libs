using Notify.Models;
using Notify.Models.Responses;

namespace GovUK.Dfe.CoreLibs.Email.Interfaces;

/// <summary>
/// Interface for GOV.UK Notify client to enable testing
/// </summary>
public interface INotificationClient
{
    /// <summary>
    /// Sends an email using a template
    /// </summary>
    EmailNotificationResponse SendEmail(string emailAddress, string templateId, Dictionary<string, dynamic>? personalisation = null, string? clientReference = null, string? emailReplyToId = null);

    /// <summary>
    /// Gets a notification by ID
    /// </summary>
    Notification GetNotificationById(string notificationId);

    /// <summary>
    /// Gets notifications with optional filters
    /// </summary>
    NotificationList GetNotifications(string? templateType = null, string? status = null, string? reference = null, string? olderThanId = null);

    /// <summary>
    /// Gets a template by ID
    /// </summary>
    TemplateResponse GetTemplateById(string templateId);

    /// <summary>
    /// Gets a template by ID and version
    /// </summary>
    TemplateResponse GetTemplateByIdAndVersion(string templateId, int version);

    /// <summary>
    /// Gets all templates
    /// </summary>
    TemplateList GetAllTemplates(string? templateType = null);

    /// <summary>
    /// Generates a template preview
    /// </summary>
    TemplatePreviewResponse GenerateTemplatePreview(string templateId, Dictionary<string, dynamic>? personalisation = null);
}

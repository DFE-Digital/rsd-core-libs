using GovUK.Dfe.CoreLibs.Email.Interfaces;
using Notify.Client;
using Notify.Models;
using Notify.Models.Responses;

namespace GovUK.Dfe.CoreLibs.Email.Services;

/// <summary>
/// Wrapper around the GOV.UK Notify NotificationClient to enable testing
/// </summary>
public class NotificationClientWrapper : INotificationClient
{
    private readonly NotificationClient _client;

    /// <summary>
    /// Creates a new notification client wrapper
    /// </summary>
    /// <param name="apiKey">GOV.UK Notify API key</param>
    public NotificationClientWrapper(string apiKey)
    {
        _client = new NotificationClient(apiKey);
    }

    /// <inheritdoc />
    public EmailNotificationResponse SendEmail(string emailAddress, string templateId, Dictionary<string, dynamic>? personalisation = null, string? clientReference = null, string? emailReplyToId = null)
    {
        return _client.SendEmail(emailAddress, templateId, personalisation, clientReference, emailReplyToId);
    }

    /// <inheritdoc />
    public Notification GetNotificationById(string notificationId)
    {
        return _client.GetNotificationById(notificationId);
    }

    /// <inheritdoc />
    public NotificationList GetNotifications(string? templateType = null, string? status = null, string? reference = null, string? olderThanId = null)
    {
        return _client.GetNotifications(templateType, status, reference, olderThanId);
    }

    /// <inheritdoc />
    public TemplateResponse GetTemplateById(string templateId)
    {
        return _client.GetTemplateById(templateId);
    }

    /// <inheritdoc />
    public TemplateResponse GetTemplateByIdAndVersion(string templateId, int version)
    {
        return _client.GetTemplateByIdAndVersion(templateId, version);
    }

    /// <inheritdoc />
    public TemplateList GetAllTemplates(string? templateType = null)
    {
        return _client.GetAllTemplates(templateType);
    }

    /// <inheritdoc />
    public TemplatePreviewResponse GenerateTemplatePreview(string templateId, Dictionary<string, dynamic>? personalisation = null)
    {
        return _client.GenerateTemplatePreview(templateId, personalisation);
    }
}

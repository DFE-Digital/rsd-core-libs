using System.Text;
using GovUK.Dfe.CoreLibs.Email.Exceptions;
using GovUK.Dfe.CoreLibs.Email.Interfaces;
using GovUK.Dfe.CoreLibs.Email.Models;
using GovUK.Dfe.CoreLibs.Email.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notify.Exceptions;
using Notify.Models.Responses;

namespace GovUK.Dfe.CoreLibs.Email.Providers;

/// <summary>
/// GOV.UK Notify email provider implementation
/// </summary>
public class GovUkNotifyEmailProvider : IEmailProvider
{
    private readonly INotificationClient _notifyClient;
    private readonly GovUkNotifyOptions _options;
    private readonly ILogger<GovUkNotifyEmailProvider> _logger;

    /// <summary>
    /// Creates a new GOV.UK Notify email provider
    /// </summary>
    /// <param name="options">GOV.UK Notify options</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="notificationClient">Notification client for API calls</param>
    public GovUkNotifyEmailProvider(IOptions<EmailOptions> options, ILogger<GovUkNotifyEmailProvider> logger, INotificationClient notificationClient)
    {
        ArgumentNullException.ThrowIfNull(options);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _notifyClient = notificationClient ?? throw new ArgumentNullException(nameof(notificationClient));
        
        _options = options.Value.GovUkNotify;
        
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new EmailConfigurationException("GOV.UK Notify API key is required");
        }

        _logger.LogInformation("GOV.UK Notify email provider initialized");
    }

    /// <inheritdoc />
    public string ProviderName => "GovUkNotify";

    /// <inheritdoc />
    public bool SupportsAttachments => false;

    /// <inheritdoc />
    public bool SupportsTemplates => true;

    /// <inheritdoc />
    public bool SupportsStatusTracking => true;

    /// <inheritdoc />
    public bool SupportsMultipleRecipients => false;

    /// <inheritdoc />
    public async Task<EmailResponse> SendEmailAsync(EmailMessage emailMessage, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check for attachments (not supported in this version)
            if (emailMessage.Attachments?.Any() == true)
            {
                throw new EmailValidationException("File attachments are not currently supported with GOV.UK Notify provider.");
            }

            EmailNotificationResponse response;

            if (!string.IsNullOrWhiteSpace(emailMessage.TemplateId))
            {
                // Send template-based email
                response = await SendTemplateEmailAsync(emailMessage, cancellationToken);
            }
            else
            {
                throw new EmailValidationException("GOV.UK Notify requires a template ID. Plain text emails are not supported.");
            }

            return MapToEmailResponse(response);
        }
        catch (NotifyClientException ex)
        {
            _logger.LogError(ex, "GOV.UK Notify client error sending email");
            throw new EmailProviderException($"GOV.UK Notify error: {ex.Message}", ProviderName);
        }
        catch (Exception ex) when (!(ex is EmailException))
        {
            _logger.LogError(ex, "Unexpected error sending email via GOV.UK Notify");
            throw new EmailProviderException($"Unexpected error: {ex.Message}", ex, ProviderName);
        }
    }

    /// <inheritdoc />
    public async Task<EmailResponse> GetEmailStatusAsync(string emailId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await Task.Run(() => _notifyClient.GetNotificationById(emailId), cancellationToken);
            return MapToEmailResponse(response);
        }
        catch (NotifyClientException ex)
        {
            _logger.LogError(ex, "GOV.UK Notify client error getting email status for ID: {EmailId}", emailId);
            throw new EmailProviderException($"GOV.UK Notify error: {ex.Message}", ProviderName);
        }
        catch (Exception ex) when (!(ex is EmailException))
        {
            _logger.LogError(ex, "Unexpected error getting email status for ID: {EmailId}", emailId);
            throw new EmailProviderException($"Unexpected error: {ex.Message}", ex, ProviderName);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<EmailResponse>> GetEmailsAsync(string? reference = null, EmailStatus? status = null, DateTime? olderThan = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var notifyStatus = status.HasValue ? MapToNotifyStatus(status.Value) : null;
            
            var response = await Task.Run(() => _notifyClient.GetNotifications(
                templateType: "email",
                status: notifyStatus,
                reference: reference
            ), cancellationToken);

            return response.notifications.Select(MapToEmailResponse);
        }
        catch (NotifyClientException ex)
        {
            _logger.LogError(ex, "GOV.UK Notify client error getting emails");
            throw new EmailProviderException($"GOV.UK Notify error: {ex.Message}", ProviderName);
        }
        catch (Exception ex) when (!(ex is EmailException))
        {
            _logger.LogError(ex, "Unexpected error getting emails from GOV.UK Notify");
            throw new EmailProviderException($"Unexpected error: {ex.Message}", ex, ProviderName);
        }
    }

    /// <inheritdoc />
    public async Task<EmailTemplate> GetTemplateAsync(string templateId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(templateId);
        
        try
        {
            var response = await Task.Run(() => _notifyClient.GetTemplateById(templateId), cancellationToken);
            return MapToEmailTemplate(response);
        }
        catch (NotifyClientException ex)
        {
            _logger.LogError(ex, "GOV.UK Notify client error getting template {TemplateId}", templateId);
            throw new EmailProviderException($"GOV.UK Notify error: {ex.Message}", ProviderName);
        }
        catch (Exception ex) when (!(ex is EmailException))
        {
            _logger.LogError(ex, "Unexpected error getting template {TemplateId}", templateId);
            throw new EmailProviderException($"Unexpected error: {ex.Message}", ex, ProviderName);
        }
    }

    /// <inheritdoc />
    public async Task<EmailTemplate> GetTemplateAsync(string templateId, int version, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(templateId);
        
        try
        {
            var response = await Task.Run(() => _notifyClient.GetTemplateByIdAndVersion(templateId, version), cancellationToken);
            return MapToEmailTemplate(response);
        }
        catch (NotifyClientException ex)
        {
            _logger.LogError(ex, "GOV.UK Notify client error getting template {TemplateId} version {Version}", templateId, version);
            throw new EmailProviderException($"GOV.UK Notify error: {ex.Message}", ProviderName);
        }
        catch (Exception ex) when (!(ex is EmailException))
        {
            _logger.LogError(ex, "Unexpected error getting template {TemplateId} version {Version}", templateId, version);
            throw new EmailProviderException($"Unexpected error: {ex.Message}", ex, ProviderName);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<EmailTemplate>> GetAllTemplatesAsync(string? templateType = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await Task.Run(() => _notifyClient.GetAllTemplates(templateType ?? "email"), cancellationToken);
            return response.templates.Select(MapToEmailTemplate);
        }
        catch (NotifyClientException ex)
        {
            _logger.LogError(ex, "GOV.UK Notify client error getting all templates");
            throw new EmailProviderException($"GOV.UK Notify error: {ex.Message}", ProviderName);
        }
        catch (Exception ex) when (!(ex is EmailException))
        {
            _logger.LogError(ex, "Unexpected error getting all templates");
            throw new EmailProviderException($"Unexpected error: {ex.Message}", ex, ProviderName);
        }
    }

    /// <inheritdoc />
    public async Task<TemplatePreview> PreviewTemplateAsync(string templateId, Dictionary<string, object>? personalization = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(templateId);
        
        try
        {
            var personalDict = personalization?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value as dynamic ?? kvp.Value.ToString() ?? "");
            
            var response = await Task.Run(() => _notifyClient.GenerateTemplatePreview(templateId, personalDict), cancellationToken);
            return MapToTemplatePreview(response);
        }
        catch (NotifyClientException ex)
        {
            _logger.LogError(ex, "GOV.UK Notify client error previewing template {TemplateId}", templateId);
            throw new EmailProviderException($"GOV.UK Notify error: {ex.Message}", ProviderName);
        }
        catch (Exception ex) when (!(ex is EmailException))
        {
            _logger.LogError(ex, "Unexpected error previewing template {TemplateId}", templateId);
            throw new EmailProviderException($"Unexpected error: {ex.Message}", ex, ProviderName);
        }
    }

    private async Task<EmailNotificationResponse> SendTemplateEmailAsync(EmailMessage emailMessage, CancellationToken cancellationToken)
    {
        var personalDict = emailMessage.Personalization?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value as dynamic ?? kvp.Value.ToString() ?? "");

        // Get the primary recipient (GOV.UK Notify only supports one recipient per call)
        var primaryRecipient = emailMessage.GetPrimaryRecipient();
        if (string.IsNullOrWhiteSpace(primaryRecipient))
        {
            throw new EmailValidationException("No valid recipient email address found");
        }

        // Send regular template email
        return await Task.Run(() => _notifyClient.SendEmail(
            emailAddress: primaryRecipient,
            templateId: emailMessage.TemplateId!,
            personalisation: personalDict,
            clientReference: emailMessage.Reference,
            emailReplyToId: null
        ), cancellationToken);
    }


    private static EmailResponse MapToEmailResponse(EmailNotificationResponse response)
    {
        return new EmailResponse
        {
            Id = response.id,
            Reference = response.reference,
            Uri = response.uri,
            Status = EmailStatus.Sent, // Default status for sent emails
            Template = response.template != null ? new EmailTemplate
            {
                Id = response.template.id,
                Version = response.template.version,
                Uri = response.template.uri
            } : null,
            Content = response.content != null ? new EmailContent
            {
                Subject = response.content.subject,
                Body = response.content.body,
                FromEmail = response.content.fromEmail
            } : null,
            CreatedAt = DateTime.UtcNow, // Use current time as fallback
            SentAt = null,
            CompletedAt = null
        };
    }

    private static EmailResponse MapToEmailResponse(Notify.Models.Notification response)
    {
        return new EmailResponse
        {
            Id = response.id,
            Reference = response.reference,
            Uri = null, // Uri property may not be available in this model
            Status = MapFromNotifyStatus(response.status),
            Template = response.template != null ? new EmailTemplate
            {
                Id = response.template.id,
                Version = response.template.version,
                Uri = null
            } : null,
            CreatedAt = TryParseDateTime(response.createdAt),
            SentAt = TryParseDateTimeNullable(response.sentAt),
            CompletedAt = TryParseDateTimeNullable(response.completedAt)
        };
    }

    private static EmailTemplate MapToEmailTemplate(TemplateResponse response)
    {
        return new EmailTemplate
        {
            Id = response.id,
            Name = response.name,
            Type = response.type,
            Version = response.version,
            Uri = null, // Uri property may not be available
            Subject = response.subject,
            Body = response.body
        };
    }

    private static TemplatePreview MapToTemplatePreview(TemplatePreviewResponse response)
    {
        return new TemplatePreview
        {
            Id = response.id,
            Version = response.version,
            Type = response.type,
            Subject = response.subject,
            Body = response.body,
            Html = null // Html property may not be available
        };
    }

    private static DateTime TryParseDateTime(string? dateTimeString)
    {
        if (string.IsNullOrEmpty(dateTimeString))
            return DateTime.UtcNow;

        if (DateTime.TryParse(dateTimeString, out var result))
            return result;

        return DateTime.UtcNow;
    }

    private static DateTime? TryParseDateTimeNullable(string? dateTimeString)
    {
        if (string.IsNullOrEmpty(dateTimeString))
            return null;

        if (DateTime.TryParse(dateTimeString, out var result))
            return result;

        return null;
    }

    private static EmailStatus MapFromNotifyStatus(string notifyStatus)
    {
        return notifyStatus?.ToLowerInvariant() switch
        {
            "created" => EmailStatus.Created,
            "sending" => EmailStatus.Sending,
            "pending" => EmailStatus.Queued,
            "sent" => EmailStatus.Sent,
            "delivered" => EmailStatus.Delivered,
            "permanent-failure" => EmailStatus.PermanentFailure,
            "temporary-failure" => EmailStatus.TemporaryFailure,
            "technical-failure" => EmailStatus.TechnicalFailure,
            "accepted" => EmailStatus.Accepted,
            _ => EmailStatus.Unknown
        };
    }

    private static string? MapToNotifyStatus(EmailStatus status)
    {
        return status switch
        {
            EmailStatus.Created => "created",
            EmailStatus.Sending => "sending",
            EmailStatus.Queued => "pending",
            EmailStatus.Sent => "sent",
            EmailStatus.Delivered => "delivered",
            EmailStatus.PermanentFailure => "permanent-failure",
            EmailStatus.TemporaryFailure => "temporary-failure",
            EmailStatus.TechnicalFailure => "technical-failure",
            EmailStatus.Accepted => "accepted",
            _ => null
        };
    }
}

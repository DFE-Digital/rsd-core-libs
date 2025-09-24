using System.Net.Mail;
using GovUK.Dfe.CoreLibs.Email.Exceptions;
using GovUK.Dfe.CoreLibs.Email.Interfaces;
using GovUK.Dfe.CoreLibs.Email.Models;
using GovUK.Dfe.CoreLibs.Email.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GovUK.Dfe.CoreLibs.Email.Services;

/// <summary>
/// Generic email service that delegates to specific email providers
/// </summary>
public class EmailService : IEmailService
{
    private readonly IEmailProvider _emailProvider;
    private readonly EmailOptions _options;
    private readonly ILogger<EmailService> _logger;


    /// <summary>
    /// Creates a new EmailService instance
    /// </summary>
    /// <param name="emailProvider">The email provider implementation</param>
    /// <param name="options">Email service options</param>
    /// <param name="logger">Logger instance</param>
    public EmailService(IEmailProvider emailProvider, IOptions<EmailOptions> options, ILogger<EmailService> logger)
    {
        _emailProvider = emailProvider ?? throw new ArgumentNullException(nameof(emailProvider));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<EmailResponse> SendEmailAsync(EmailMessage emailMessage, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(emailMessage);

        var recipients = emailMessage.GetAllRecipients();
        _logger.LogInformation("Sending email via {Provider} to {Recipients}", _emailProvider.ProviderName, string.Join(", ", recipients));

        try
        {
            // Validate email message
            ValidateEmailMessage(emailMessage);

            // Handle multiple recipients
            if (recipients.Count > 1)
            {
                return await HandleMultipleRecipientsAsync(emailMessage, recipients, cancellationToken);
            }

            // Single recipient - send normally
            var response = await _emailProvider.SendEmailAsync(emailMessage, cancellationToken);

            _logger.LogInformation("Email sent successfully via {Provider}. ID: {EmailId}", _emailProvider.ProviderName, response.Id);

            return response;
        }
        catch (Exception ex) when (!(ex is EmailException))
        {
            _logger.LogError(ex, "Unexpected error sending email via {Provider}", _emailProvider.ProviderName);
            throw new EmailProviderException($"Failed to send email: {ex.Message}", ex, _emailProvider.ProviderName);
        }
    }

    /// <inheritdoc />
    public async Task<EmailResponse> GetEmailStatusAsync(string emailId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(emailId);

        _logger.LogDebug("Getting email status for ID: {EmailId} via {Provider}", emailId, _emailProvider.ProviderName);

        try
        {
            return await _emailProvider.GetEmailStatusAsync(emailId, cancellationToken);
        }
        catch (Exception ex) when (!(ex is EmailException))
        {
            _logger.LogError(ex, "Error getting email status for ID: {EmailId} via {Provider}", emailId, _emailProvider.ProviderName);
            throw new EmailProviderException($"Failed to get email status: {ex.Message}", ex, _emailProvider.ProviderName);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<EmailResponse>> GetEmailsAsync(string? reference = null, EmailStatus? status = null, DateTime? olderThan = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting emails via {Provider} with filters: Reference={Reference}, Status={Status}, OlderThan={OlderThan}", 
            _emailProvider.ProviderName, reference, status, olderThan);

        try
        {
            return await _emailProvider.GetEmailsAsync(reference, status, olderThan, cancellationToken);
        }
        catch (Exception ex) when (!(ex is EmailException))
        {
            _logger.LogError(ex, "Error getting emails via {Provider}", _emailProvider.ProviderName);
            throw new EmailProviderException($"Failed to get emails: {ex.Message}", ex, _emailProvider.ProviderName);
        }
    }

    /// <inheritdoc />
    public async Task<EmailTemplate> GetTemplateAsync(string templateId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(templateId);

        if (!_emailProvider.SupportsTemplates)
        {
            throw new EmailProviderException($"Provider {_emailProvider.ProviderName} does not support templates", _emailProvider.ProviderName);
        }

        _logger.LogDebug("Getting template {TemplateId} via {Provider}", templateId, _emailProvider.ProviderName);

        try
        {
            return await _emailProvider.GetTemplateAsync(templateId, cancellationToken);
        }
        catch (Exception ex) when (!(ex is EmailException))
        {
            _logger.LogError(ex, "Error getting template {TemplateId} via {Provider}", templateId, _emailProvider.ProviderName);
            throw new EmailProviderException($"Failed to get template: {ex.Message}", ex, _emailProvider.ProviderName);
        }
    }

    /// <inheritdoc />
    public async Task<EmailTemplate> GetTemplateAsync(string templateId, int version, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(templateId);

        if (!_emailProvider.SupportsTemplates)
        {
            throw new EmailProviderException($"Provider {_emailProvider.ProviderName} does not support templates", _emailProvider.ProviderName);
        }

        _logger.LogDebug("Getting template {TemplateId} version {Version} via {Provider}", templateId, version, _emailProvider.ProviderName);

        try
        {
            return await _emailProvider.GetTemplateAsync(templateId, version, cancellationToken);
        }
        catch (Exception ex) when (!(ex is EmailException))
        {
            _logger.LogError(ex, "Error getting template {TemplateId} version {Version} via {Provider}", templateId, version, _emailProvider.ProviderName);
            throw new EmailProviderException($"Failed to get template: {ex.Message}", ex, _emailProvider.ProviderName);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<EmailTemplate>> GetAllTemplatesAsync(string? templateType = null, CancellationToken cancellationToken = default)
    {
        if (!_emailProvider.SupportsTemplates)
        {
            throw new EmailProviderException($"Provider {_emailProvider.ProviderName} does not support templates", _emailProvider.ProviderName);
        }

        _logger.LogDebug("Getting all templates via {Provider} with type filter: {TemplateType}", _emailProvider.ProviderName, templateType);

        try
        {
            return await _emailProvider.GetAllTemplatesAsync(templateType, cancellationToken);
        }
        catch (Exception ex) when (!(ex is EmailException))
        {
            _logger.LogError(ex, "Error getting all templates via {Provider}", _emailProvider.ProviderName);
            throw new EmailProviderException($"Failed to get templates: {ex.Message}", ex, _emailProvider.ProviderName);
        }
    }

    /// <inheritdoc />
    public async Task<TemplatePreview> PreviewTemplateAsync(string templateId, Dictionary<string, object>? personalization = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(templateId);

        if (!_emailProvider.SupportsTemplates)
        {
            throw new EmailProviderException($"Provider {_emailProvider.ProviderName} does not support templates", _emailProvider.ProviderName);
        }

        _logger.LogDebug("Previewing template {TemplateId} via {Provider}", templateId, _emailProvider.ProviderName);

        try
        {
            return await _emailProvider.PreviewTemplateAsync(templateId, personalization, cancellationToken);
        }
        catch (Exception ex) when (!(ex is EmailException))
        {
            _logger.LogError(ex, "Error previewing template {TemplateId} via {Provider}", templateId, _emailProvider.ProviderName);
            throw new EmailProviderException($"Failed to preview template: {ex.Message}", ex, _emailProvider.ProviderName);
        }
    }

    /// <inheritdoc />
    public bool IsValidEmail(string emailAddress)
    {
        if (string.IsNullOrWhiteSpace(emailAddress))
            return false;

        try
        {
            var mail = new MailAddress(emailAddress);
            return mail.Address == emailAddress;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public string ProviderName => _emailProvider.ProviderName;

    private async Task<EmailResponse> HandleMultipleRecipientsAsync(EmailMessage emailMessage, List<string> recipients, CancellationToken cancellationToken)
    {
        if (_emailProvider.SupportsMultipleRecipients)
        {
            // Provider supports multiple recipients natively
            _logger.LogDebug("Provider {Provider} supports multiple recipients, sending in single call", _emailProvider.ProviderName);
            var response = await _emailProvider.SendEmailAsync(emailMessage, cancellationToken);
            response.Recipients = recipients;
            return response;
        }
        else
        {
            // Provider doesn't support multiple recipients - send individual emails
            _logger.LogDebug("Provider {Provider} doesn't support multiple recipients, sending {Count} individual emails", _emailProvider.ProviderName, recipients.Count);
            
            var tasks = recipients.Select(async recipient =>
            {
                var individualMessage = new EmailMessage
                {
                    ToEmail = recipient,
                    Subject = emailMessage.Subject,
                    Body = emailMessage.Body,
                    TemplateId = emailMessage.TemplateId,
                    Personalization = emailMessage.Personalization,
                    Attachments = emailMessage.Attachments,
                    Reference = emailMessage.Reference,
                    ReplyToEmail = emailMessage.ReplyToEmail
                };

                try
                {
                    return await _emailProvider.SendEmailAsync(individualMessage, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email to {Recipient}", recipient);
                    // Return a failed response for this recipient
                    return new EmailResponse
                    {
                        Id = $"failed-{Guid.NewGuid()}",
                        Status = EmailStatus.PermanentFailure,
                        CreatedAt = DateTime.UtcNow,
                        Recipients = new List<string> { recipient },
                        Metadata = new Dictionary<string, object> { ["error"] = ex.Message }
                    };
                }
            });

            var responses = await Task.WhenAll(tasks);
            
            // Create a combined response
            var successfulResponses = responses.Where(r => r.Status != EmailStatus.PermanentFailure).ToList();
            var failedCount = responses.Length - successfulResponses.Count;

            _logger.LogInformation("Multi-recipient email completed: {Success} successful, {Failed} failed", successfulResponses.Count, failedCount);

            return new EmailResponse
            {
                Id = $"multi-{Guid.NewGuid()}",
                Reference = emailMessage.Reference,
                Status = failedCount == 0 ? EmailStatus.Sent : (successfulResponses.Any() ? EmailStatus.Sent : EmailStatus.PermanentFailure),
                CreatedAt = DateTime.UtcNow,
                Recipients = recipients,
                RecipientResponses = responses.ToList(),
                Metadata = new Dictionary<string, object>
                {
                    ["total_recipients"] = recipients.Count,
                    ["successful_count"] = successfulResponses.Count,
                    ["failed_count"] = failedCount,
                    ["provider_supports_multiple"] = false
                }
            };
        }
    }

    private void ValidateEmailMessage(EmailMessage emailMessage)
    {
        if (!_options.EnableValidation)
            return;

        var errors = new List<string>();
        var recipients = emailMessage.GetAllRecipients();

        // Validate that we have at least one recipient
        if (!recipients.Any())
        {
            errors.Add("At least one recipient email address is required (ToEmail or ToEmails)");
        }
        else
        {
            // Validate all recipient emails
            foreach (var email in recipients)
            {
                if (!IsValidEmail(email))
                {
                    errors.Add($"Email '{email}' is not a valid email address");
                }
            }
        }

        // Validate template vs content
        if (string.IsNullOrWhiteSpace(emailMessage.TemplateId))
        {
            // Non-template email - require subject and body
            if (string.IsNullOrWhiteSpace(emailMessage.Subject))
                errors.Add("Subject is required when not using a template");

            if (string.IsNullOrWhiteSpace(emailMessage.Body))
                errors.Add("Body is required when not using a template");
        }
        else
        {
            // Template email - check provider support
            if (!_emailProvider.SupportsTemplates)
                errors.Add($"Provider {_emailProvider.ProviderName} does not support templates");
        }

        // Validate attachments
        if (emailMessage.Attachments?.Any() == true)
        {
            if (!_emailProvider.SupportsAttachments)
            {
                errors.Add($"Provider {_emailProvider.ProviderName} does not support attachments");
            }
            else
            {
                foreach (var attachment in emailMessage.Attachments)
                {
                    if (string.IsNullOrWhiteSpace(attachment.FileName))
                        errors.Add("Attachment filename is required");

                    if (attachment.Content == null || attachment.Content.Length == 0)
                        errors.Add($"Attachment '{attachment.FileName}' has no content");
                }
            }
        }

        // Validate reply-to email
        if (!string.IsNullOrWhiteSpace(emailMessage.ReplyToEmail) && !IsValidEmail(emailMessage.ReplyToEmail))
        {
            errors.Add($"ReplyToEmail '{emailMessage.ReplyToEmail}' is not a valid email address");
        }

        // Validate multiple recipients support
        if (recipients.Count > 1 && !_emailProvider.SupportsMultipleRecipients)
        {
            _logger.LogDebug("Provider {Provider} doesn't support multiple recipients natively, will send individual emails", _emailProvider.ProviderName);
        }

        if (errors.Any() && _options.ThrowOnValidationError)
        {
            throw new EmailValidationException($"Email validation failed: {string.Join(", ", errors)}");
        }
    }
}

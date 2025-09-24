using GovUK.Dfe.CoreLibs.Email.Models;

namespace GovUK.Dfe.CoreLibs.Email.Interfaces;

/// <summary>
/// Generic email service interface that provides a unified way to send emails
/// regardless of the underlying email provider
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email using the configured email provider
    /// </summary>
    /// <param name="emailMessage">The email message to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Email response with send status and tracking information</returns>
    Task<EmailResponse> SendEmailAsync(EmailMessage emailMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of a previously sent email
    /// </summary>
    /// <param name="emailId">The email identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current email status and details</returns>
    Task<EmailResponse> GetEmailStatusAsync(string emailId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets multiple email statuses
    /// </summary>
    /// <param name="reference">Optional reference to filter by</param>
    /// <param name="status">Optional status to filter by</param>
    /// <param name="olderThan">Optional date filter for emails older than specified date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of email responses</returns>
    Task<IEnumerable<EmailResponse>> GetEmailsAsync(string? reference = null, EmailStatus? status = null, DateTime? olderThan = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a template by ID
    /// </summary>
    /// <param name="templateId">Template identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Template information</returns>
    Task<EmailTemplate> GetTemplateAsync(string templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific version of a template
    /// </summary>
    /// <param name="templateId">Template identifier</param>
    /// <param name="version">Template version</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Template information</returns>
    Task<EmailTemplate> GetTemplateAsync(string templateId, int version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available templates
    /// </summary>
    /// <param name="templateType">Optional filter by template type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of templates</returns>
    Task<IEnumerable<EmailTemplate>> GetAllTemplatesAsync(string? templateType = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a preview of a template with personalization applied
    /// </summary>
    /// <param name="templateId">Template identifier</param>
    /// <param name="personalization">Personalization data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Template preview</returns>
    Task<TemplatePreview> PreviewTemplateAsync(string templateId, Dictionary<string, object>? personalization = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an email address format
    /// </summary>
    /// <param name="emailAddress">Email address to validate</param>
    /// <returns>True if email format is valid</returns>
    bool IsValidEmail(string emailAddress);

    /// <summary>
    /// Gets the provider name for this email service
    /// </summary>
    string ProviderName { get; }
}

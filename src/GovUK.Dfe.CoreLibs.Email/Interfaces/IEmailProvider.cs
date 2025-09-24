using GovUK.Dfe.CoreLibs.Email.Models;

namespace GovUK.Dfe.CoreLibs.Email.Interfaces;

/// <summary>
/// Interface for specific email provider implementations
/// </summary>
public interface IEmailProvider
{
    /// <summary>
    /// Sends an email using this provider's specific implementation
    /// </summary>
    /// <param name="emailMessage">The email message to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Email response with provider-specific details</returns>
    Task<EmailResponse> SendEmailAsync(EmailMessage emailMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of an email using provider-specific implementation
    /// </summary>
    /// <param name="emailId">The email identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current email status</returns>
    Task<EmailResponse> GetEmailStatusAsync(string emailId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets multiple emails with optional filtering
    /// </summary>
    /// <param name="reference">Optional reference filter</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="olderThan">Optional date filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of emails</returns>
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
    /// Generates a preview of a template
    /// </summary>
    /// <param name="templateId">Template identifier</param>
    /// <param name="personalization">Personalization data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Template preview</returns>
    Task<TemplatePreview> PreviewTemplateAsync(string templateId, Dictionary<string, object>? personalization = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Provider name identifier
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Indicates if this provider supports file attachments
    /// </summary>
    bool SupportsAttachments { get; }

    /// <summary>
    /// Indicates if this provider supports email templates
    /// </summary>
    bool SupportsTemplates { get; }

    /// <summary>
    /// Indicates if this provider supports email status tracking
    /// </summary>
    bool SupportsStatusTracking { get; }

    /// <summary>
    /// Indicates if this provider supports sending to multiple recipients in a single call
    /// </summary>
    bool SupportsMultipleRecipients { get; }
}

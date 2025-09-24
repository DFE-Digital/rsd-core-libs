# GovUK.Dfe.CoreLibs.Email

A flexible and extensible email library for .NET applications that provides a unified interface for sending emails through multiple providers. The library supports template-based emails, file attachments, email status tracking, and is designed to be easily extended with new email providers.

## Features

- **Multiple Email Providers**: Currently supports GOV.UK Notify with easy extensibility for other providers
- **Template Support**: Full support for template-based emails with personalization
- **Multiple Recipients**: Send the same email to multiple recipients with intelligent handling
- **File Attachments**: Send files with emails (provider dependent)
- **Status Tracking**: Track email delivery status and get detailed information
- **Validation**: Built-in email validation and error handling
- **Async/Await**: Fully asynchronous API with cancellation token support
- **Dependency Injection**: Native support for ASP.NET Core DI container
- **Flexible Configuration**: Support for configuration via appsettings.json or explicit options
- **Comprehensive Logging**: Built-in logging support for monitoring and debugging

## Quick Start

### 1. Installation

```bash
dotnet add package GovUK.Dfe.CoreLibs.Email
```

### 2. Configuration

Add email configuration to your `appsettings.json`:

```json
{
  "Email": {
    "Provider": "GovUkNotify",
    "EnableValidation": true,
    "ThrowOnValidationError": true,
    "TimeoutSeconds": 30,
    "RetryAttempts": 3,
    "GovUkNotify": {
      "ApiKey": "your-govuk-notify-api-key",
      "TimeoutSeconds": 30,
      "MaxAttachmentSize": 2097152,
      "AllowedAttachmentTypes": [".pdf", ".csv", ".txt", ".doc", ".docx", ".xls", ".xlsx"]
    }
  }
}
```

### 3. Register Services

In your `Program.cs` or `Startup.cs`:

```csharp
// Using configuration from appsettings.json
services.AddEmailServices(configuration);

// Or register with GOV.UK Notify specifically
services.AddEmailServicesWithGovUkNotify(configuration);

// Or register with explicit API key
services.AddEmailServicesWithGovUkNotify("your-api-key");
```

### 4. Use the Service

```csharp
public class EmailController : ControllerBase
{
    private readonly IEmailService _emailService;

    public EmailController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendEmail()
    {
        var emailMessage = new EmailMessage
        {
            ToEmail = "user@example.com",
            TemplateId = "your-template-id",
            Personalization = new Dictionary<string, object>
            {
                ["name"] = "John Doe",
                ["reference_number"] = "REF123456"
            },
            Reference = "my-reference"
        };

        try
        {
            var response = await _emailService.SendEmailAsync(emailMessage);
            return Ok(new { EmailId = response.Id, Status = response.Status });
        }
        catch (EmailException ex)
        {
            return BadRequest(new { Error = ex.Message, ErrorCode = ex.ErrorCode });
        }
    }
}
```

## Email Providers

### GOV.UK Notify

The library includes full support for [GOV.UK Notify](https://www.notifications.service.gov.uk/) with the following features:

- **Template-based emails**: Send emails using pre-defined templates
- **Personalization**: Dynamic content with template variables
- **File attachments**: Send documents with emails (one per email)
- **Status tracking**: Get delivery status and detailed information
- **Template management**: Retrieve and preview templates

#### GOV.UK Notify Configuration

```json
{
  "Email": {
    "Provider": "GovUkNotify",
    "GovUkNotify": {
      "ApiKey": "your-govuk-notify-api-key",
      "BaseUrl": "https://api.notifications.service.gov.uk", // Optional
      "TimeoutSeconds": 30,
      "UseProxy": false,
      "MaxAttachmentSize": 2097152, // 2MB
      "AllowedAttachmentTypes": [".pdf", ".csv", ".txt", ".doc", ".docx", ".xls", ".xlsx", ".rtf", ".odt", ".ods", ".odp"]
    }
  }
}
```

#### Multiple Recipients Support

**GOV.UK Notify**: Does not support multiple recipients natively. When you send to multiple recipients, the library automatically sends individual API calls for each recipient in parallel. This ensures:

- ✅ **Reliability**: If one recipient fails, others still receive the email
- ✅ **Tracking**: Each recipient gets a separate tracking ID
- ✅ **Performance**: Parallel sending for better speed
- ✅ **Transparency**: You get detailed status for each recipient

**Future Providers**: Providers like SendGrid, Amazon SES, etc., may support native multiple recipients, which would be used automatically when available.

#### API Key Types

GOV.UK Notify supports different API key types:

- **Test keys**: For testing (prefix: `test-`)
- **Team and guest list keys**: For team members during development
- **Live keys**: For production use

## Usage Examples

### Sending Template-Based Email

```csharp
var emailMessage = new EmailMessage
{
    ToEmail = "recipient@example.com",
    TemplateId = "template-uuid",
    Personalization = new Dictionary<string, object>
    {
        ["user_name"] = "Jane Smith",
        ["login_url"] = "https://example.com/login",
        ["expiry_date"] = DateTime.Now.AddDays(7).ToString("d MMMM yyyy")
    },
    Reference = "user-login-notification"
};

var response = await emailService.SendEmailAsync(emailMessage);
```

### Sending Email to Multiple Recipients

You can send the same email to multiple recipients in several ways:

#### Option 1: Using ToEmails property
```csharp
var emailMessage = new EmailMessage
{
    ToEmails = new List<string> 
    { 
        "user1@example.com", 
        "user2@example.com", 
        "admin@example.com" 
    },
    TemplateId = "notification-template",
    Personalization = new Dictionary<string, object>
    {
        ["message"] = "System maintenance scheduled for tonight"
    }
};

var response = await emailService.SendEmailAsync(emailMessage);
```

#### Option 2: Using both ToEmail and ToEmails
```csharp
var emailMessage = new EmailMessage
{
    ToEmail = "primary@example.com",      // Primary recipient
    ToEmails = new List<string>           // Additional recipients
    { 
        "backup@example.com", 
        "admin@example.com" 
    },
    TemplateId = "alert-template"
};

var response = await emailService.SendEmailAsync(emailMessage);
// All three recipients will receive the same email
```

#### Handling Multiple Recipients Response
```csharp
var response = await emailService.SendEmailAsync(multiRecipientMessage);

Console.WriteLine($"Total recipients: {response.Recipients?.Count}");
Console.WriteLine($"Status: {response.Status}");

if (response.RecipientResponses?.Any() == true)
{
    foreach (var recipientResponse in response.RecipientResponses)
    {
        Console.WriteLine($"Recipient: {recipientResponse.Recipients?.FirstOrDefault()}, Status: {recipientResponse.Status}");
    }
}

// Check metadata for summary
if (response.Metadata != null)
{
    Console.WriteLine($"Successful: {response.Metadata["successful_count"]}");
    Console.WriteLine($"Failed: {response.Metadata["failed_count"]}");
}
```

### Sending Email with Attachment

```csharp
var attachment = new EmailAttachment
{
    FileName = "report.pdf",
    Content = File.ReadAllBytes("path/to/report.pdf"),
    ContentType = "application/pdf"
};

var emailMessage = new EmailMessage
{
    ToEmail = "recipient@example.com",
    TemplateId = "template-with-attachment",
    Personalization = new Dictionary<string, object>
    {
        ["recipient_name"] = "John Doe"
    },
    Attachments = new List<EmailAttachment> { attachment }
};

var response = await emailService.SendEmailAsync(emailMessage);
```

### Tracking Email Status

```csharp
// Get status of a specific email
var emailStatus = await emailService.GetEmailStatusAsync("email-id");
Console.WriteLine($"Status: {emailStatus.Status}, Sent: {emailStatus.SentAt}");

// Get multiple emails with filtering
var emails = await emailService.GetEmailsAsync(
    reference: "user-notifications",
    status: EmailStatus.Delivered,
    olderThan: DateTime.Now.AddDays(-7)
);
```

### Working with Templates

```csharp
// Get a template
var template = await emailService.GetTemplateAsync("template-id");

// Get all templates
var allTemplates = await emailService.GetAllTemplatesAsync("email");

// Preview a template with personalization
var preview = await emailService.PreviewTemplateAsync("template-id", new Dictionary<string, object>
{
    ["name"] = "Preview User",
    ["date"] = DateTime.Now.ToString("d MMMM yyyy")
});

Console.WriteLine($"Subject: {preview.Subject}");
Console.WriteLine($"Body: {preview.Body}");
```

## Configuration Options

### EmailOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| Provider | string | Required | Email provider to use ("GovUkNotify") |
| DefaultFromEmail | string? | null | Default sender email address |
| DefaultFromName | string? | null | Default sender name |
| EnableValidation | bool | true | Enable email validation |
| TimeoutSeconds | int | 30 | Operation timeout in seconds |
| RetryAttempts | int | 3 | Number of retry attempts |
| ThrowOnValidationError | bool | true | Throw exceptions on validation errors |

### GovUkNotifyOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| ApiKey | string? | Required | GOV.UK Notify API key |
| BaseUrl | string? | null | Custom API base URL (optional) |
| TimeoutSeconds | int | 30 | HTTP client timeout |
| UseProxy | bool | false | Enable proxy support |
| MaxAttachmentSize | long | 2MB | Maximum attachment size in bytes |
| AllowedAttachmentTypes | List<string> | See below | Allowed file extensions |

**Default Allowed Attachment Types**: `.pdf`, `.csv`, `.txt`, `.doc`, `.docx`, `.xls`, `.xlsx`, `.rtf`, `.odt`, `.ods`, `.odp`

## Email Status Values

| Status | Description |
|--------|-------------|
| Created | Email created but not yet processed |
| Queued | Email queued for sending |
| Sending | Email is being sent |
| Sent | Email has been sent |
| Delivered | Email was delivered successfully |
| Accepted | Email was accepted by the provider |
| TemporaryFailure | Temporary delivery failure (will retry) |
| PermanentFailure | Permanent delivery failure |
| TechnicalFailure | Technical error occurred |
| Unknown | Status could not be determined |

## Error Handling

The library provides comprehensive error handling with specific exception types:

### EmailException

Base exception for all email-related errors.

### EmailValidationException

Thrown when email validation fails (invalid email addresses, missing required fields, etc.).

### EmailConfigurationException

Thrown when configuration is invalid or missing.

### EmailProviderException

Thrown when the email provider encounters an error (API errors, network issues, etc.).

### Example Error Handling

```csharp
try
{
    var response = await emailService.SendEmailAsync(emailMessage);
    // Handle success
}
catch (EmailValidationException ex)
{
    // Handle validation errors
    logger.LogWarning("Email validation failed: {Message}", ex.Message);
    return BadRequest($"Invalid email: {ex.Message}");
}
catch (EmailProviderException ex)
{
    // Handle provider-specific errors
    logger.LogError(ex, "Email provider error: {Message} (Status: {StatusCode})", ex.Message, ex.StatusCode);
    return StatusCode(500, "Email service unavailable");
}
catch (EmailConfigurationException ex)
{
    // Handle configuration errors
    logger.LogError(ex, "Email configuration error: {Message}", ex.Message);
    return StatusCode(500, "Email service misconfigured");
}
```

## Advanced Configuration

### Custom Email Provider

You can implement your own email provider by implementing the `IEmailProvider` interface:

```csharp
public class CustomEmailProvider : IEmailProvider
{
    public string ProviderName => "CustomProvider";
    public bool SupportsAttachments => true;
    public bool SupportsTemplates => false;
    public bool SupportsStatusTracking => true;

    public async Task<EmailResponse> SendEmailAsync(EmailMessage emailMessage, CancellationToken cancellationToken = default)
    {
        // Your implementation
    }

    // Implement other required methods...
}

// Register your custom provider
services.AddEmailServicesWithCustomProvider<CustomEmailProvider>(configuration);
```

### Environment-Specific Configuration

```csharp
// Development configuration
if (environment.IsDevelopment())
{
    services.AddEmailServicesWithGovUkNotify(testApiKey, options =>
    {
        options.ThrowOnValidationError = false;
        options.EnableValidation = true;
    });
}
else
{
    // Production configuration
    services.AddEmailServicesWithGovUkNotify(configuration);
}
```

## Testing

The library is designed to be easily testable. You can mock the `IEmailService` interface for unit tests:

```csharp
[Test]
public async Task SendEmail_ShouldReturnSuccessResponse()
{
    // Arrange
    var mockEmailService = new Mock<IEmailService>();
    mockEmailService
        .Setup(x => x.SendEmailAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(new EmailResponse
        {
            Id = "test-id",
            Status = EmailStatus.Sent,
            CreatedAt = DateTime.UtcNow
        });

    var controller = new EmailController(mockEmailService.Object);

    // Act
    var result = await controller.SendEmail(emailMessage);

    // Assert
    Assert.IsInstanceOf<OkResult>(result);
}
```

For integration testing with GOV.UK Notify, use test API keys and test email addresses provided in their documentation.

## Performance Considerations

- The library uses async/await throughout for better scalability
- HTTP connections are managed by the underlying HttpClient
- Consider implementing retry policies for production environments
- Use cancellation tokens for better resource management
- Monitor attachment sizes to avoid performance issues

## Security

- Store API keys securely (Azure Key Vault, environment variables, etc.)
- Validate email addresses to prevent injection attacks
- Use appropriate API key types (test/team/live) for different environments
- Implement rate limiting if sending high volumes of emails
- Log security-relevant events for monitoring

## Troubleshooting

### Common Issues

1. **"GOV.UK Notify API key is required"**
   - Ensure the API key is properly configured in appsettings.json
   - Check that the configuration section name matches

2. **"Provider does not support templates"**
   - Verify you're using a provider that supports templates (GOV.UK Notify does)
   - Check your configuration

3. **"Attachment type not allowed"**
   - Check the file extension against the allowed types
   - Configure custom allowed types if needed

4. **Rate limiting errors**
   - GOV.UK Notify has rate limits (3,000 per minute)
   - Implement retry logic with exponential backoff

### Logging

Enable detailed logging to troubleshoot issues:

```json
{
  "Logging": {
    "LogLevel": {
      "GovUK.Dfe.CoreLibs.Email": "Debug"
    }
  }
}
```

## Changelog

### Version 1.0.0
- Initial release with GOV.UK Notify provider support
- Template-based email sending
- File attachment support
- Email status tracking
- Comprehensive validation and error handling

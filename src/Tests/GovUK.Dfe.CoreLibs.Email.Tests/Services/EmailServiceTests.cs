using AutoFixture;
using GovUK.Dfe.CoreLibs.Email.Exceptions;
using GovUK.Dfe.CoreLibs.Email.Interfaces;
using GovUK.Dfe.CoreLibs.Email.Models;
using GovUK.Dfe.CoreLibs.Email.Services;
using GovUK.Dfe.CoreLibs.Email.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GovUK.Dfe.CoreLibs.Email.Tests.Services;

public class EmailServiceTests
{
    private readonly IFixture _fixture;
    private readonly IEmailProvider _mockProvider;
    private readonly IOptions<EmailOptions> _mockOptions;
    private readonly ILogger<EmailService> _mockLogger;
    private readonly EmailService _emailService;
    private readonly EmailOptions _emailOptions;

    public EmailServiceTests()
    {
        _fixture = new Fixture();
        _mockProvider = Substitute.For<IEmailProvider>();
        _mockOptions = Substitute.For<IOptions<EmailOptions>>();
        _mockLogger = Substitute.For<ILogger<EmailService>>();

        _emailOptions = new EmailOptions
        {
            Provider = "TestProvider",
            EnableValidation = true,
            ThrowOnValidationError = true,
            TimeoutSeconds = 30,
            RetryAttempts = 3
        };

        _mockOptions.Value.Returns(_emailOptions);
        _mockProvider.ProviderName.Returns("TestProvider");
        _mockProvider.SupportsTemplates.Returns(true);
        _mockProvider.SupportsAttachments.Returns(false);
        _mockProvider.SupportsStatusTracking.Returns(true);
        _mockProvider.SupportsMultipleRecipients.Returns(false);

        _emailService = new EmailService(_mockProvider, _mockOptions, _mockLogger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullProvider_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new EmailService(null!, _mockOptions, _mockLogger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("emailProvider");
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new EmailService(_mockProvider, null!, _mockLogger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new EmailService(_mockProvider, _mockOptions, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region SendEmailAsync Tests

    [Fact]
    public async Task SendEmailAsync_WithNullEmailMessage_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = async () => await _emailService.SendEmailAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("emailMessage");
    }

    [Fact]
    public async Task SendEmailAsync_WithValidSingleRecipient_ShouldCallProviderAndReturnResponse()
    {
        // Arrange
        var emailMessage = new EmailMessage
        {
            ToEmail = "test@example.com",
            TemplateId = "template-123",
            Personalization = new Dictionary<string, object> { ["name"] = "Test User" }
        };

        var expectedResponse = new EmailResponse
        {
            Id = "email-123",
            Status = EmailStatus.Sent,
            CreatedAt = DateTime.UtcNow
        };

        _mockProvider.SendEmailAsync(emailMessage, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _emailService.SendEmailAsync(emailMessage);

        // Assert
        result.Should().BeSameAs(expectedResponse);
        await _mockProvider.Received(1).SendEmailAsync(emailMessage, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendEmailAsync_WithMultipleRecipientsAndProviderSupportsMultiple_ShouldCallProviderOnce()
    {
        // Arrange
        _mockProvider.SupportsMultipleRecipients.Returns(true);
        
        var emailMessage = new EmailMessage
        {
            ToEmails = new List<string> { "test1@example.com", "test2@example.com" },
            TemplateId = "template-123"
        };

        var expectedResponse = new EmailResponse
        {
            Id = "email-123",
            Status = EmailStatus.Sent,
            CreatedAt = DateTime.UtcNow
        };

        _mockProvider.SendEmailAsync(emailMessage, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _emailService.SendEmailAsync(emailMessage);

        // Assert
        result.Should().BeSameAs(expectedResponse);
        result.Recipients.Should().Equal("test1@example.com", "test2@example.com");
        await _mockProvider.Received(1).SendEmailAsync(emailMessage, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendEmailAsync_WithMultipleRecipientsAndProviderDoesNotSupportMultiple_ShouldSendIndividualEmails()
    {
        // Arrange
        var emailMessage = new EmailMessage
        {
            ToEmails = new List<string> { "test1@example.com", "test2@example.com" },
            TemplateId = "template-123",
            Reference = "test-ref"
        };

        var response1 = new EmailResponse
        {
            Id = "email-1",
            Status = EmailStatus.Sent,
            CreatedAt = DateTime.UtcNow
        };

        var response2 = new EmailResponse
        {
            Id = "email-2",
            Status = EmailStatus.Sent,
            CreatedAt = DateTime.UtcNow
        };

        _mockProvider.SendEmailAsync(Arg.Is<EmailMessage>(e => e.ToEmail == "test1@example.com"), Arg.Any<CancellationToken>())
            .Returns(response1);
        
        _mockProvider.SendEmailAsync(Arg.Is<EmailMessage>(e => e.ToEmail == "test2@example.com"), Arg.Any<CancellationToken>())
            .Returns(response2);

        // Act
        var result = await _emailService.SendEmailAsync(emailMessage);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(EmailStatus.Sent);
        result.Recipients.Should().Equal("test1@example.com", "test2@example.com");
        result.RecipientResponses.Should().HaveCount(2);
        result.Metadata.Should().ContainKey("successful_count").WhoseValue.Should().Be(2);
        result.Metadata.Should().ContainKey("failed_count").WhoseValue.Should().Be(0);
        
        await _mockProvider.Received(2).SendEmailAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendEmailAsync_WithMultipleRecipientsAndSomeFailures_ShouldHandlePartialSuccess()
    {
        // Arrange
        var emailMessage = new EmailMessage
        {
            ToEmails = new List<string> { "test1@example.com", "test2@example.com", "test3@example.com" },
            TemplateId = "template-123"
        };

        var successResponse = new EmailResponse
        {
            Id = "email-success",
            Status = EmailStatus.Sent,
            CreatedAt = DateTime.UtcNow
        };

        _mockProvider.SendEmailAsync(Arg.Is<EmailMessage>(e => e.ToEmail == "test1@example.com"), Arg.Any<CancellationToken>())
            .Returns(successResponse);
        
        _mockProvider.SendEmailAsync(Arg.Is<EmailMessage>(e => e.ToEmail == "test2@example.com"), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<EmailResponse>(new EmailProviderException("Failed to send", "TestProvider")));
        
        _mockProvider.SendEmailAsync(Arg.Is<EmailMessage>(e => e.ToEmail == "test3@example.com"), Arg.Any<CancellationToken>())
            .Returns(successResponse);

        // Act
        var result = await _emailService.SendEmailAsync(emailMessage);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(EmailStatus.Sent); // Partial success
        result.Recipients.Should().Equal("test1@example.com", "test2@example.com", "test3@example.com");
        result.RecipientResponses.Should().HaveCount(3);
        result.Metadata.Should().ContainKey("successful_count").WhoseValue.Should().Be(2);
        result.Metadata.Should().ContainKey("failed_count").WhoseValue.Should().Be(1);
    }

    [Fact]
    public async Task SendEmailAsync_WithProviderException_ShouldWrapInEmailProviderException()
    {
        // Arrange
        var emailMessage = new EmailMessage
        {
            ToEmail = "test@example.com",
            TemplateId = "template-123"
        };

        _mockProvider.SendEmailAsync(emailMessage, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<EmailResponse>(new InvalidOperationException("Provider error")));

        // Act & Assert
        var act = async () => await _emailService.SendEmailAsync(emailMessage);
        await act.Should().ThrowAsync<EmailProviderException>()
            .WithMessage("Failed to send email: Provider error");
    }

    #endregion

    #region Validation Tests

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task SendEmailAsync_WithInvalidEmail_ShouldThrowEmailValidationException(string invalidEmail)
    {
        // Arrange
        var emailMessage = new EmailMessage
        {
            ToEmail = invalidEmail,
            TemplateId = "template-123"
        };

        // Act & Assert
        var act = async () => await _emailService.SendEmailAsync(emailMessage);
        await act.Should().ThrowAsync<EmailValidationException>();
    }

    [Fact]
    public async Task SendEmailAsync_WithInvalidEmailFormat_ShouldThrowEmailValidationException()
    {
        // Arrange
        var emailMessage = new EmailMessage
        {
            ToEmail = "invalid-email-format",
            TemplateId = "template-123"
        };

        // Act & Assert
        var act = async () => await _emailService.SendEmailAsync(emailMessage);
        await act.Should().ThrowAsync<EmailValidationException>()
            .WithMessage("*not a valid email address*");
    }

    [Fact]
    public async Task SendEmailAsync_WithNoRecipients_ShouldThrowEmailValidationException()
    {
        // Arrange
        var emailMessage = new EmailMessage
        {
            TemplateId = "template-123"
        };

        // Act & Assert
        var act = async () => await _emailService.SendEmailAsync(emailMessage);
        await act.Should().ThrowAsync<EmailValidationException>()
            .WithMessage("*At least one recipient email address is required*");
    }

    [Fact]
    public async Task SendEmailAsync_WithTemplateButProviderDoesNotSupportTemplates_ShouldThrowEmailValidationException()
    {
        // Arrange
        _mockProvider.SupportsTemplates.Returns(false);
        
        var emailMessage = new EmailMessage
        {
            ToEmail = "test@example.com",
            TemplateId = "template-123"
        };

        // Act & Assert
        var act = async () => await _emailService.SendEmailAsync(emailMessage);
        await act.Should().ThrowAsync<EmailValidationException>()
            .WithMessage("*does not support templates*");
    }

    [Fact]
    public async Task SendEmailAsync_WithAttachmentsButProviderDoesNotSupportAttachments_ShouldThrowEmailValidationException()
    {
        // Arrange
        var emailMessage = new EmailMessage
        {
            ToEmail = "test@example.com",
            Subject = "Test",
            Body = "Test body",
            Attachments = new List<EmailAttachment>
            {
                new() { FileName = "test.pdf", Content = new byte[] { 1, 2, 3 } }
            }
        };

        // Act & Assert
        var act = async () => await _emailService.SendEmailAsync(emailMessage);
        await act.Should().ThrowAsync<EmailValidationException>()
            .WithMessage("*does not support attachments*");
    }

    [Fact]
    public async Task SendEmailAsync_WithoutTemplateAndMissingSubject_ShouldThrowEmailValidationException()
    {
        // Arrange
        var emailMessage = new EmailMessage
        {
            ToEmail = "test@example.com",
            Body = "Test body"
            // Missing Subject
        };

        // Act & Assert
        var act = async () => await _emailService.SendEmailAsync(emailMessage);
        await act.Should().ThrowAsync<EmailValidationException>()
            .WithMessage("*Subject is required when not using a template*");
    }

    [Fact]
    public async Task SendEmailAsync_WithoutTemplateAndMissingBody_ShouldThrowEmailValidationException()
    {
        // Arrange
        var emailMessage = new EmailMessage
        {
            ToEmail = "test@example.com",
            Subject = "Test Subject"
            // Missing Body
        };

        // Act & Assert
        var act = async () => await _emailService.SendEmailAsync(emailMessage);
        await act.Should().ThrowAsync<EmailValidationException>()
            .WithMessage("*Body is required when not using a template*");
    }

    [Fact]
    public async Task SendEmailAsync_WithValidationDisabled_ShouldNotValidate()
    {
        // Arrange
        _emailOptions.EnableValidation = false;
        
        var emailMessage = new EmailMessage
        {
            ToEmail = "invalid-email-format",
            TemplateId = "template-123"
        };

        var expectedResponse = new EmailResponse
        {
            Id = "email-123",
            Status = EmailStatus.Sent,
            CreatedAt = DateTime.UtcNow
        };

        _mockProvider.SendEmailAsync(emailMessage, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _emailService.SendEmailAsync(emailMessage);

        // Assert
        result.Should().BeSameAs(expectedResponse);
    }

    #endregion

    #region Template Tests

    [Fact]
    public async Task GetTemplateAsync_WithValidId_ShouldReturnTemplate()
    {
        // Arrange
        const string templateId = "template-123";
        var expectedTemplate = new EmailTemplate
        {
            Id = templateId,
            Version = 1,
            Subject = "Test Template"
        };

        _mockProvider.GetTemplateAsync(templateId, Arg.Any<CancellationToken>())
            .Returns(expectedTemplate);

        // Act
        var result = await _emailService.GetTemplateAsync(templateId);

        // Assert
        result.Should().BeSameAs(expectedTemplate);
    }

    [Fact]
    public async Task GetTemplateAsync_WithProviderNotSupportingTemplates_ShouldThrowEmailProviderException()
    {
        // Arrange
        _mockProvider.SupportsTemplates.Returns(false);

        // Act & Assert
        var act = async () => await _emailService.GetTemplateAsync("template-123");
        await act.Should().ThrowAsync<EmailProviderException>()
            .WithMessage("*does not support templates*");
    }

    [Fact]
    public async Task GetTemplateAsync_WithVersionSpecified_ShouldCallProviderWithVersion()
    {
        // Arrange
        const string templateId = "template-123";
        const int version = 2;
        var expectedTemplate = new EmailTemplate
        {
            Id = templateId,
            Version = version
        };

        _mockProvider.GetTemplateAsync(templateId, version, Arg.Any<CancellationToken>())
            .Returns(expectedTemplate);

        // Act
        var result = await _emailService.GetTemplateAsync(templateId, version);

        // Assert
        result.Should().BeSameAs(expectedTemplate);
        await _mockProvider.Received(1).GetTemplateAsync(templateId, version, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Email Validation Tests

    [Theory]
    [InlineData("test@example.com", true)]
    [InlineData("user.name@domain.co.uk", true)]
    [InlineData("user+tag@example.org", true)]
    [InlineData("invalid-email", false)]
    [InlineData("@example.com", false)]
    [InlineData("test@", false)]
    [InlineData("test..email@example.com", true)]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData(null, false)]
    public void IsValidEmail_WithVariousEmails_ShouldReturnExpectedResult(string email, bool expected)
    {
        // Act
        var result = _emailService.IsValidEmail(email);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Provider Name Tests

    [Fact]
    public void ProviderName_ShouldReturnProviderName()
    {
        // Act
        var providerName = _emailService.ProviderName;

        // Assert
        providerName.Should().Be("TestProvider");
    }

    #endregion

    #region Status Tracking Tests

    [Fact]
    public async Task GetEmailStatusAsync_WithNullEmailId_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = async () => await _emailService.GetEmailStatusAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("emailId");
    }

    [Fact]
    public async Task GetEmailStatusAsync_WithValidEmailId_ShouldReturnStatus()
    {
        // Arrange
        const string emailId = "email-123";
        var expectedResponse = new EmailResponse
        {
            Id = emailId,
            Status = EmailStatus.Delivered,
            CreatedAt = DateTime.UtcNow
        };

        _mockProvider.GetEmailStatusAsync(emailId, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _emailService.GetEmailStatusAsync(emailId);

        // Assert
        result.Should().BeSameAs(expectedResponse);
    }

    [Fact]
    public async Task GetEmailsAsync_ShouldCallProviderWithFilters()
    {
        // Arrange
        const string reference = "test-ref";
        const EmailStatus status = EmailStatus.Sent;
        var olderThan = DateTime.UtcNow.AddDays(-1);

        var expectedEmails = new List<EmailResponse>
        {
            new() { Id = "email-1", Status = EmailStatus.Sent, CreatedAt = DateTime.UtcNow },
            new() { Id = "email-2", Status = EmailStatus.Sent, CreatedAt = DateTime.UtcNow }
        };

        _mockProvider.GetEmailsAsync(reference, status, olderThan, Arg.Any<CancellationToken>())
            .Returns(expectedEmails);

        // Act
        var result = await _emailService.GetEmailsAsync(reference, status, olderThan);

        // Assert
        result.Should().BeSameAs(expectedEmails);
    }

    #endregion

    #region Additional Coverage Tests

    [Fact]
    public async Task PreviewTemplateAsync_WithValidParameters_ShouldCallProvider()
    {
        // Arrange
        var expectedPreview = new TemplatePreview
        {
            Id = "template-123",
            Type = "email",
            Version = 1,
            Body = "Hello John",
            Subject = "Welcome John"
        };

        var personalization = new Dictionary<string, object>
        {
            ["name"] = "John"
        };

        _mockProvider.PreviewTemplateAsync("template-123", personalization, Arg.Any<CancellationToken>())
            .Returns(expectedPreview);

        // Act
        var result = await _emailService.PreviewTemplateAsync("template-123", personalization);

        // Assert
        result.Should().Be(expectedPreview);
        await _mockProvider.Received(1).PreviewTemplateAsync("template-123", personalization, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PreviewTemplateAsync_WithNullTemplateId_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = async () => await _emailService.PreviewTemplateAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("templateId");
    }

    [Fact]
    public async Task PreviewTemplateAsync_WhenProviderDoesNotSupportTemplates_ShouldThrowEmailProviderException()
    {
        // Arrange
        _mockProvider.SupportsTemplates.Returns(false);

        // Act & Assert
        var act = async () => await _emailService.PreviewTemplateAsync("template-123");
        await act.Should().ThrowAsync<EmailProviderException>()
            .WithMessage("Email provider TestProvider does not support template previews");
    }

    [Fact]
    public async Task GetAllTemplatesAsync_ShouldCallProvider()
    {
        // Arrange
        var expectedTemplates = new List<EmailTemplate>
        {
            new EmailTemplate { Id = "template-1", Name = "Template 1" },
            new EmailTemplate { Id = "template-2", Name = "Template 2" }
        };

        _mockProvider.GetAllTemplatesAsync(null, Arg.Any<CancellationToken>())
            .Returns(expectedTemplates);

        // Act
        var result = await _emailService.GetAllTemplatesAsync();

        // Assert
        result.Should().BeEquivalentTo(expectedTemplates);
        await _mockProvider.Received(1).GetAllTemplatesAsync(null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllTemplatesAsync_WhenProviderDoesNotSupportTemplates_ShouldThrowEmailProviderException()
    {
        // Arrange
        _mockProvider.SupportsTemplates.Returns(false);

        // Act & Assert
        var act = async () => await _emailService.GetAllTemplatesAsync();
        await act.Should().ThrowAsync<EmailProviderException>()
            .WithMessage("Email provider TestProvider does not support templates");
    }

    [Fact]
    public async Task SendEmailAsync_WithNullPersonalization_ShouldHandleCorrectly()
    {
        // Arrange
        var emailMessage = new EmailMessage
        {
            ToEmail = "test@example.com",
            TemplateId = "template-123",
            Personalization = null
        };

        var expectedResponse = new EmailResponse { Id = "test-id", Status = EmailStatus.Sent };
        _mockProvider.SendEmailAsync(emailMessage, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _emailService.SendEmailAsync(emailMessage);

        // Assert
        result.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task SendEmailAsync_WithEmptyPersonalization_ShouldHandleCorrectly()
    {
        // Arrange
        var emailMessage = new EmailMessage
        {
            ToEmail = "test@example.com",
            TemplateId = "template-123",
            Personalization = new Dictionary<string, object>()
        };

        var expectedResponse = new EmailResponse { Id = "test-id", Status = EmailStatus.Sent };
        _mockProvider.SendEmailAsync(emailMessage, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _emailService.SendEmailAsync(emailMessage);

        // Assert
        result.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task SendEmailAsync_WithReference_ShouldPassToProvider()
    {
        // Arrange
        var emailMessage = new EmailMessage
        {
            ToEmail = "test@example.com",
            TemplateId = "template-123",
            Reference = "my-reference"
        };

        var expectedResponse = new EmailResponse { Id = "test-id", Status = EmailStatus.Sent };
        _mockProvider.SendEmailAsync(emailMessage, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _emailService.SendEmailAsync(emailMessage);

        // Assert
        result.Should().Be(expectedResponse);
        await _mockProvider.Received(1).SendEmailAsync(
            Arg.Is<EmailMessage>(e => e.Reference == "my-reference"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendEmailAsync_WithReplyToEmail_ShouldPassToProvider()
    {
        // Arrange
        var emailMessage = new EmailMessage
        {
            ToEmail = "test@example.com",
            TemplateId = "template-123",
            ReplyToEmail = "reply@example.com"
        };

        var expectedResponse = new EmailResponse { Id = "test-id", Status = EmailStatus.Sent };
        _mockProvider.SendEmailAsync(emailMessage, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _emailService.SendEmailAsync(emailMessage);

        // Assert
        result.Should().Be(expectedResponse);
        await _mockProvider.Received(1).SendEmailAsync(
            Arg.Is<EmailMessage>(e => e.ReplyToEmail == "reply@example.com"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetEmailsAsync_WithNullFilters_ShouldCallProviderWithNulls()
    {
        // Arrange
        var expectedEmails = new List<EmailResponse>
        {
            new EmailResponse { Id = "email-1" },
            new EmailResponse { Id = "email-2" }
        };

        _mockProvider.GetEmailsAsync(null, null, null, Arg.Any<CancellationToken>())
            .Returns(expectedEmails);

        // Act
        var result = await _emailService.GetEmailsAsync();

        // Assert
        result.Should().BeEquivalentTo(expectedEmails);
        await _mockProvider.Received(1).GetEmailsAsync(null, null, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetEmailsAsync_WithAllFilters_ShouldPassToProvider()
    {
        // Arrange
        var expectedEmails = new List<EmailResponse>
        {
            new EmailResponse { Id = "email-1", Status = EmailStatus.Delivered }
        };

        _mockProvider.GetEmailsAsync("test-ref", EmailStatus.Delivered, null, Arg.Any<CancellationToken>())
            .Returns(expectedEmails);

        // Act
        var result = await _emailService.GetEmailsAsync("test-ref", EmailStatus.Delivered);

        // Assert
        result.Should().BeEquivalentTo(expectedEmails);
        await _mockProvider.Received(1).GetEmailsAsync("test-ref", EmailStatus.Delivered, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void IsValidEmail_WithNullOrWhitespaceEmails_ShouldReturnFalse()
    {
        // Act & Assert
        _emailService.IsValidEmail(null!).Should().BeFalse();
        _emailService.IsValidEmail("").Should().BeFalse();
        _emailService.IsValidEmail("   ").Should().BeFalse();
        _emailService.IsValidEmail("\t").Should().BeFalse();
        _emailService.IsValidEmail("\n").Should().BeFalse();
    }

    [Fact]
    public void IsValidEmail_WithValidEmails_ShouldReturnTrue()
    {
        // Act & Assert
        _emailService.IsValidEmail("test@example.com").Should().BeTrue();
        _emailService.IsValidEmail("user.name@domain.co.uk").Should().BeTrue();
        _emailService.IsValidEmail("user+tag@example.org").Should().BeTrue();
        _emailService.IsValidEmail("a@b.co").Should().BeTrue();
    }

    [Fact]
    public void IsValidEmail_WithInvalidEmails_ShouldReturnFalse()
    {
        // Act & Assert
        _emailService.IsValidEmail("invalid-email").Should().BeFalse();
        _emailService.IsValidEmail("@example.com").Should().BeFalse();
        _emailService.IsValidEmail("test@").Should().BeFalse();
        _emailService.IsValidEmail("test.example.com").Should().BeFalse();
        _emailService.IsValidEmail("test@.com").Should().BeFalse();
    }

    #endregion
}

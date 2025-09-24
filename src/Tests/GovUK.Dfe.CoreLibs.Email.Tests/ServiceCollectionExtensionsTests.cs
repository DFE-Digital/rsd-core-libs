using GovUK.Dfe.CoreLibs.Email;
using GovUK.Dfe.CoreLibs.Email.Exceptions;
using GovUK.Dfe.CoreLibs.Email.Interfaces;
using GovUK.Dfe.CoreLibs.Email.Models;
using GovUK.Dfe.CoreLibs.Email.Providers;
using GovUK.Dfe.CoreLibs.Email.Services;
using GovUK.Dfe.CoreLibs.Email.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GovUK.Dfe.CoreLibs.Email.Tests;

public class ServiceCollectionExtensionsTests
{
    private readonly IServiceCollection _services;
    private readonly IConfiguration _configuration;

    public ServiceCollectionExtensionsTests()
    {
        _services = new ServiceCollection();
        
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Email:Provider"] = "GovUkNotify",
            ["Email:EnableValidation"] = "true",
            ["Email:TimeoutSeconds"] = "30",
            ["Email:RetryAttempts"] = "3",
            ["Email:GovUkNotify:ApiKey"] = "test_key-00000000-0000-0000-0000-000000000000-00000000-0000-0000-0000-000000000000",
            ["Email:GovUkNotify:TimeoutSeconds"] = "30",
            ["Email:GovUkNotify:MaxAttachmentSize"] = "2097152"
        });
        _configuration = configBuilder.Build();
    }

    #region AddEmailServices Configuration Tests

    [Fact]
    public void AddEmailServices_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => ServiceCollectionExtensions.AddEmailServices(null!, _configuration);
        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddEmailServices_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => _services.AddEmailServices((IConfiguration)null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("configuration");
    }

    [Fact]
    public void AddEmailServices_WithValidConfiguration_ShouldRegisterServices()
    {
        // Act
        _services.AddEmailServices(_configuration);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        
        serviceProvider.GetService<IEmailService>().Should().NotBeNull();
        serviceProvider.GetService<IEmailProvider>().Should().NotBeNull();
        serviceProvider.GetService<IEmailProvider>().Should().BeOfType<GovUkNotifyEmailProvider>();
        
        var emailService = serviceProvider.GetService<IEmailService>();
        emailService.Should().BeOfType<EmailService>();
        emailService!.ProviderName.Should().Be("GovUkNotify");
    }

    [Fact]
    public void AddEmailServices_WithExplicitOptions_ShouldRegisterServices()
    {
        // Arrange
        var emailOptions = new EmailOptions
        {
            Provider = "GovUkNotify",
            EnableValidation = true,
            TimeoutSeconds = 45,
            RetryAttempts = 5,
            GovUkNotify = new GovUkNotifyOptions
            {
                ApiKey = "test_key-00000000-0000-0000-0000-000000000000-00000000-0000-0000-0000-000000000000"
            }
        };

        // Act
        _services.AddEmailServices(emailOptions);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        
        var options = serviceProvider.GetService<IOptions<EmailOptions>>();
        options.Should().NotBeNull();
        options!.Value.Provider.Should().Be("GovUkNotify");
        options.Value.TimeoutSeconds.Should().Be(45);
        options.Value.RetryAttempts.Should().Be(5);
        options.Value.GovUkNotify.ApiKey.Should().Be("test_key-00000000-0000-0000-0000-000000000000-00000000-0000-0000-0000-000000000000");
    }

    #endregion

    #region AddEmailServicesWithGovUkNotify Tests

    [Fact]
    public void AddEmailServicesWithGovUkNotify_WithConfiguration_ShouldRegisterGovUkNotifyProvider()
    {
        // Act
        _services.AddEmailServicesWithGovUkNotify(_configuration);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        
        var emailProvider = serviceProvider.GetService<IEmailProvider>();
        emailProvider.Should().BeOfType<GovUkNotifyEmailProvider>();
        emailProvider!.ProviderName.Should().Be("GovUkNotify");
    }

    [Fact]
    public void AddEmailServicesWithGovUkNotify_WithExplicitApiKey_ShouldRegisterCorrectly()
    {
        // Arrange
        const string apiKey = "test_key-00000000-0000-0000-0000-000000000000-00000000-0000-0000-0000-000000000000";

        // Act
        _services.AddEmailServicesWithGovUkNotify(apiKey, null);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        
        var options = serviceProvider.GetService<IOptions<EmailOptions>>();
        options.Should().NotBeNull();
        options!.Value.Provider.Should().Be("GovUkNotify");
        options.Value.GovUkNotify.ApiKey.Should().Be(apiKey);
    }

    [Fact]
    public void AddEmailServicesWithGovUkNotify_WithApiKeyAndConfigureAction_ShouldApplyConfiguration()
    {
        // Arrange
        const string apiKey = "test_key-00000000-0000-0000-0000-000000000000-00000000-0000-0000-0000-000000000000";

        // Act
        _services.AddEmailServicesWithGovUkNotify(apiKey, options =>
        {
            options.EnableValidation = false;
            options.TimeoutSeconds = 60;
            options.DefaultFromEmail = "noreply@example.com";
        });

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        
        var options = serviceProvider.GetService<IOptions<EmailOptions>>();
        options.Should().NotBeNull();
        options!.Value.EnableValidation.Should().BeFalse();
        options.Value.TimeoutSeconds.Should().Be(60);
        options.Value.DefaultFromEmail.Should().Be("noreply@example.com");
        options.Value.GovUkNotify.ApiKey.Should().Be(apiKey);
    }

    [Fact]
    public void AddEmailServicesWithGovUkNotify_WithNullApiKey_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => _services.AddEmailServicesWithGovUkNotify((string)null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("apiKey");
    }

    #endregion

    #region AddEmailServicesWithCustomProvider Tests

    [Fact]
    public void AddEmailServicesWithCustomProvider_WithValidProvider_ShouldRegisterCustomProvider()
    {
        // Act
        _services.AddEmailServicesWithCustomProvider<TestEmailProvider>(_configuration);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        
        var emailProvider = serviceProvider.GetService<IEmailProvider>();
        emailProvider.Should().BeOfType<TestEmailProvider>();
    }

    [Fact]
    public void AddEmailServicesWithCustomProvider_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => ServiceCollectionExtensions.AddEmailServicesWithCustomProvider<TestEmailProvider>(null!, _configuration);
        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddEmailServicesWithCustomProvider_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => _services.AddEmailServicesWithCustomProvider<TestEmailProvider>(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("configuration");
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void AddEmailServices_WithMissingProvider_ShouldThrowEmailConfigurationException()
    {
        // Arrange
        var invalidConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Missing Provider
                ["Email:EnableValidation"] = "true"
            })
            .Build();

        // Act & Assert
        var act = () => _services.AddEmailServices(invalidConfig);
        act.Should().Throw<EmailConfigurationException>()
            .WithMessage("Email provider is required. Set Email:Provider in configuration.");
    }

    [Fact]
    public void AddEmailServices_WithInvalidTimeoutSeconds_ShouldThrowEmailConfigurationException()
    {
        // Arrange
        var emailOptions = new EmailOptions
        {
            Provider = "GovUkNotify",
            TimeoutSeconds = -1,
            GovUkNotify = new GovUkNotifyOptions { ApiKey = "test_key-00000000-0000-0000-0000-000000000000-00000000-0000-0000-0000-000000000000" }
        };

        // Act & Assert
        var act = () => _services.AddEmailServices(emailOptions);
        act.Should().Throw<EmailConfigurationException>()
            .WithMessage("TimeoutSeconds must be greater than 0.");
    }

    [Fact]
    public void AddEmailServices_WithInvalidRetryAttempts_ShouldThrowEmailConfigurationException()
    {
        // Arrange
        var emailOptions = new EmailOptions
        {
            Provider = "GovUkNotify",
            RetryAttempts = -1,
            GovUkNotify = new GovUkNotifyOptions { ApiKey = "test_key-00000000-0000-0000-0000-000000000000-00000000-0000-0000-0000-000000000000" }
        };

        // Act & Assert
        var act = () => _services.AddEmailServices(emailOptions);
        act.Should().Throw<EmailConfigurationException>()
            .WithMessage("RetryAttempts must be 0 or greater.");
    }

    [Fact]
    public void AddEmailServices_WithUnsupportedProvider_ShouldThrowEmailConfigurationException()
    {
        // Arrange
        var emailOptions = new EmailOptions
        {
            Provider = "UnsupportedProvider",
            GovUkNotify = new GovUkNotifyOptions { ApiKey = "test_key-00000000-0000-0000-0000-000000000000-00000000-0000-0000-0000-000000000000" }
        };

        // Act & Assert
        var act = () => _services.AddEmailServices(emailOptions);
        act.Should().Throw<EmailConfigurationException>()
            .WithMessage("Unsupported email provider: UnsupportedProvider. Supported providers: GovUkNotify");
    }

    [Fact]
    public void AddEmailServices_WithGovUkNotifyButMissingApiKey_ShouldThrowEmailConfigurationException()
    {
        // Arrange
        var emailOptions = new EmailOptions
        {
            Provider = "GovUkNotify",
            GovUkNotify = new GovUkNotifyOptions() // Missing ApiKey
        };

        // Act & Assert
        var act = () => _services.AddEmailServices(emailOptions);
        act.Should().Throw<EmailConfigurationException>()
            .WithMessage("GOV.UK Notify API key is required. Set Email:GovUkNotify:ApiKey in configuration.");
    }

    [Fact]
    public void AddEmailServices_WithGovUkNotifyInvalidTimeoutSeconds_ShouldThrowEmailConfigurationException()
    {
        // Arrange
        var emailOptions = new EmailOptions
        {
            Provider = "GovUkNotify",
            GovUkNotify = new GovUkNotifyOptions 
            { 
                ApiKey = "test-key",
                TimeoutSeconds = 0
            }
        };

        // Act & Assert
        var act = () => _services.AddEmailServices(emailOptions);
        act.Should().Throw<EmailConfigurationException>()
            .WithMessage("GOV.UK Notify TimeoutSeconds must be greater than 0.");
    }

    [Fact]
    public void AddEmailServices_WithGovUkNotifyInvalidMaxAttachmentSize_ShouldThrowEmailConfigurationException()
    {
        // Arrange
        var emailOptions = new EmailOptions
        {
            Provider = "GovUkNotify",
            GovUkNotify = new GovUkNotifyOptions 
            { 
                ApiKey = "test-key",
                MaxAttachmentSize = 0
            }
        };

        // Act & Assert
        var act = () => _services.AddEmailServices(emailOptions);
        act.Should().Throw<EmailConfigurationException>()
            .WithMessage("GOV.UK Notify MaxAttachmentSize must be greater than 0.");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void AddEmailServices_WithCompleteConfiguration_ShouldCreateWorkingService()
    {
        // Act
        _services.AddEmailServices(_configuration);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        
        var emailService = serviceProvider.GetService<IEmailService>();
        emailService.Should().NotBeNull();
        emailService!.ProviderName.Should().Be("GovUkNotify");
        
        // Test that service can validate emails
        emailService.IsValidEmail("test@example.com").Should().BeTrue();
        emailService.IsValidEmail("invalid-email").Should().BeFalse();
    }

    [Fact]
    public void AddEmailServices_ShouldRegisterHttpClient()
    {
        // Act
        _services.AddEmailServices(_configuration);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        
        var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
        httpClientFactory.Should().NotBeNull();
    }

    #endregion

    #region Custom Section Name Tests

    [Fact]
    public void AddEmailServices_WithCustomSectionName_ShouldUseCorrectSection()
    {
        // Arrange
        var customConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CustomEmail:Provider"] = "GovUkNotify",
                ["CustomEmail:GovUkNotify:ApiKey"] = "test_key-00000000-0000-0000-0000-000000000000-00000000-0000-0000-0000-000000000000"
            })
            .Build();

        // Act
        _services.AddEmailServices(customConfig, "CustomEmail");

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        
        var options = serviceProvider.GetService<IOptions<EmailOptions>>();
        options.Should().NotBeNull();
        options!.Value.Provider.Should().Be("GovUkNotify");
        options.Value.GovUkNotify.ApiKey.Should().Be("test_key-00000000-0000-0000-0000-000000000000-00000000-0000-0000-0000-000000000000");
    }

    [Fact]
    public void AddEmailServicesWithGovUkNotify_WithCustomSectionName_ShouldUseCorrectSection()
    {
        // Arrange
        var customConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CustomEmail:Provider"] = "GovUkNotify",
                ["CustomEmail:GovUkNotify:ApiKey"] = "test_key-00000000-0000-0000-0000-000000000000-00000000-0000-0000-0000-000000000000",
                ["CustomEmail:EnableValidation"] = "false"
            })
            .Build();

        // Act
        _services.AddEmailServicesWithGovUkNotify(customConfig, "CustomEmail");

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        
        var options = serviceProvider.GetService<IOptions<EmailOptions>>();
        options.Should().NotBeNull();
        options!.Value.Provider.Should().Be("GovUkNotify");
        options.Value.GovUkNotify.ApiKey.Should().Be("test_key-00000000-0000-0000-0000-000000000000-00000000-0000-0000-0000-000000000000");
        options.Value.EnableValidation.Should().BeFalse();
    }

    #endregion
}

// Test implementation of IEmailProvider for custom provider tests
public class TestEmailProvider : IEmailProvider
{
    public string ProviderName => "TestProvider";
    public bool SupportsAttachments => true;
    public bool SupportsTemplates => true;
    public bool SupportsStatusTracking => true;
    public bool SupportsMultipleRecipients => true;

    public Task<EmailResponse> SendEmailAsync(EmailMessage emailMessage, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new EmailResponse
        {
            Id = "test-id",
            Status = EmailStatus.Sent,
            CreatedAt = DateTime.UtcNow
        });
    }

    public Task<EmailResponse> GetEmailStatusAsync(string emailId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new EmailResponse
        {
            Id = emailId,
            Status = EmailStatus.Delivered,
            CreatedAt = DateTime.UtcNow
        });
    }

    public Task<IEnumerable<EmailResponse>> GetEmailsAsync(string? reference = null, EmailStatus? status = null, DateTime? olderThan = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Enumerable.Empty<EmailResponse>());
    }

    public Task<EmailTemplate> GetTemplateAsync(string templateId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new EmailTemplate
        {
            Id = templateId,
            Version = 1
        });
    }

    public Task<EmailTemplate> GetTemplateAsync(string templateId, int version, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new EmailTemplate
        {
            Id = templateId,
            Version = version
        });
    }

    public Task<IEnumerable<EmailTemplate>> GetAllTemplatesAsync(string? templateType = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Enumerable.Empty<EmailTemplate>());
    }

    public Task<TemplatePreview> PreviewTemplateAsync(string templateId, Dictionary<string, object>? personalization = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new TemplatePreview
        {
            Id = templateId,
            Version = 1
        });
    }
}

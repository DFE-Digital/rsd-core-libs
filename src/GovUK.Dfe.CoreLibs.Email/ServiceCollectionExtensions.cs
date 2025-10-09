using GovUK.Dfe.CoreLibs.Email.Exceptions;
using GovUK.Dfe.CoreLibs.Email.Interfaces;
using GovUK.Dfe.CoreLibs.Email.Providers;
using GovUK.Dfe.CoreLibs.Email.Services;
using GovUK.Dfe.CoreLibs.Email.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GovUK.Dfe.CoreLibs.Email;

/// <summary>
/// Extension methods for registering email services with dependency injection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers email services using configuration from appsettings
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration instance</param>
    /// <param name="configSectionName">Configuration section name (default: "Email")</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddEmailServices(this IServiceCollection services, IConfiguration configuration, string configSectionName = "Email")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Bind configuration
        var emailOptions = new EmailOptions();
        configuration.GetSection(configSectionName).Bind(emailOptions);

        return AddEmailServices(services, emailOptions);
    }

    /// <summary>
    /// Registers email services using explicit options
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="emailOptions">Email service options</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddEmailServices(this IServiceCollection services, EmailOptions emailOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(emailOptions);

        // Validate configuration
        ValidateEmailOptions(emailOptions);

        // Register options
        services.Configure<EmailOptions>(_ => 
        {
            _.Provider = emailOptions.Provider;
            _.DefaultFromEmail = emailOptions.DefaultFromEmail;
            _.DefaultFromName = emailOptions.DefaultFromName;
            _.EnableValidation = emailOptions.EnableValidation;
            _.TimeoutSeconds = emailOptions.TimeoutSeconds;
            _.RetryAttempts = emailOptions.RetryAttempts;
            _.ThrowOnValidationError = emailOptions.ThrowOnValidationError;
            _.GovUkNotify = emailOptions.GovUkNotify;
            _.Smtp = emailOptions.Smtp;
        });

        // Register provider based on configuration
        RegisterEmailProvider(services, emailOptions);

        // Register main email service
        services.AddScoped<IEmailService, EmailService>();

        // Add HTTP client for providers that need it
        services.AddHttpClient();

        return services;
    }

    /// <summary>
    /// Registers email services with GOV.UK Notify provider using configuration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration instance</param>
    /// <param name="configSectionName">Configuration section name (default: "Email")</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddEmailServicesWithGovUkNotify(this IServiceCollection services, IConfiguration configuration, string configSectionName = "Email")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var emailOptions = new EmailOptions
        {
            Provider = "GovUkNotify"
        };
        
        configuration.GetSection(configSectionName).Bind(emailOptions);

        return AddEmailServices(services, emailOptions);
    }

    /// <summary>
    /// Registers email services with GOV.UK Notify provider using explicit API key
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="apiKey">GOV.UK Notify API key</param>
    /// <param name="configureOptions">Optional action to configure additional options</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddEmailServicesWithGovUkNotify(this IServiceCollection services, string apiKey, Action<EmailOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(apiKey);

        var emailOptions = new EmailOptions
        {
            Provider = "GovUkNotify",
            GovUkNotify = new GovUkNotifyOptions
            {
                ApiKey = apiKey
            }
        };

        configureOptions?.Invoke(emailOptions);

        return AddEmailServices(services, emailOptions);
    }

    /// <summary>
    /// Registers email services with a custom email provider
    /// </summary>
    /// <typeparam name="TProvider">Email provider implementation type</typeparam>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration instance</param>
    /// <param name="configSectionName">Configuration section name (default: "Email")</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddEmailServicesWithCustomProvider<TProvider>(this IServiceCollection services, IConfiguration configuration, string configSectionName = "Email")
        where TProvider : class, IEmailProvider
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Bind configuration
        var emailOptions = new EmailOptions();
        configuration.GetSection(configSectionName).Bind(emailOptions);

        // Register options
        services.Configure<EmailOptions>(_ => 
        {
            _.Provider = emailOptions.Provider;
            _.DefaultFromEmail = emailOptions.DefaultFromEmail;
            _.DefaultFromName = emailOptions.DefaultFromName;
            _.EnableValidation = emailOptions.EnableValidation;
            _.TimeoutSeconds = emailOptions.TimeoutSeconds;
            _.RetryAttempts = emailOptions.RetryAttempts;
            _.ThrowOnValidationError = emailOptions.ThrowOnValidationError;
            _.GovUkNotify = emailOptions.GovUkNotify;
            _.Smtp = emailOptions.Smtp;
        });

        // Register custom provider
        services.AddScoped<IEmailProvider, TProvider>();

        // Register main email service
        services.AddScoped<IEmailService, EmailService>();

        // Add HTTP client
        services.AddHttpClient();

        return services;
    }

    private static void RegisterEmailProvider(IServiceCollection services, EmailOptions emailOptions)
    {
        switch (emailOptions.Provider.ToLowerInvariant())
        {
            case "govuknotify":
            case "govuk":
            case "notify":
                ValidateGovUkNotifyOptions(emailOptions.GovUkNotify);
                services.AddScoped<INotificationClient>(serviceProvider =>
                {
                    var options = serviceProvider.GetRequiredService<IOptions<EmailOptions>>();
                    return new Services.NotificationClientWrapper(options.Value.GovUkNotify.ApiKey);
                });
                services.AddScoped<IEmailProvider, GovUkNotifyEmailProvider>();
                break;

            default:
                throw new EmailConfigurationException($"Unsupported email provider: {emailOptions.Provider}. Supported providers: GovUkNotify");
        }
    }

    private static void ValidateEmailOptions(EmailOptions emailOptions)
    {
        if (string.IsNullOrWhiteSpace(emailOptions.Provider))
        {
            throw new EmailConfigurationException("Email provider is required. Set Email:Provider in configuration.");
        }

        if (emailOptions.TimeoutSeconds <= 0)
        {
            throw new EmailConfigurationException("TimeoutSeconds must be greater than 0.");
        }

        if (emailOptions.RetryAttempts < 0)
        {
            throw new EmailConfigurationException("RetryAttempts must be 0 or greater.");
        }
    }

    private static void ValidateGovUkNotifyOptions(GovUkNotifyOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new EmailConfigurationException("GOV.UK Notify API key is required. Set Email:GovUkNotify:ApiKey in configuration.");
        }

        if (options.TimeoutSeconds <= 0)
        {
            throw new EmailConfigurationException("GOV.UK Notify TimeoutSeconds must be greater than 0.");
        }

        if (options.MaxAttachmentSize <= 0)
        {
            throw new EmailConfigurationException("GOV.UK Notify MaxAttachmentSize must be greater than 0.");
        }
    }
}

using GovUK.Dfe.CoreLibs.SharePoint.Clients;
using GovUK.Dfe.CoreLibs.SharePoint.Exceptions;
using GovUK.Dfe.CoreLibs.SharePoint.Interfaces;
using GovUK.Dfe.CoreLibs.SharePoint.Services;
using GovUK.Dfe.CoreLibs.SharePoint.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GovUK.Dfe.CoreLibs.SharePoint;

/// <summary>
/// Extension methods for registering SharePoint services with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers SharePoint services using configuration from appsettings.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="configSectionName">Configuration section name (default: <c>SharePoint</c>).</param>
    /// <returns>The original <paramref name="services"/> instance.</returns>
    public static IServiceCollection AddSharePointServices(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = SharePointOptions.SectionName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new SharePointOptions();
        configuration.GetSection(configSectionName).Bind(options);

        return AddSharePointServices(services, options);
    }

    /// <summary>
    /// Registers SharePoint services using explicit options.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="options">SharePoint options.</param>
    /// <returns>The original <paramref name="services"/> instance.</returns>
    public static IServiceCollection AddSharePointServices(this IServiceCollection services, SharePointOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        ValidateOptions(options);

        services.AddSingleton(options);
        services.AddSingleton<IGraphClientWrapper>(_ => new GraphClientWrapper(options));
        services.AddSingleton<ISharePointService>(sp =>
            new SharePointService(
                sp.GetRequiredService<IGraphClientWrapper>(),
                sp.GetRequiredService<ILogger<SharePointService>>()));

        return services;
    }

    private static void ValidateOptions(SharePointOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.TenantId))
            throw new SharePointConfigurationException("SharePoint:TenantId configuration is required.");

        if (string.IsNullOrWhiteSpace(options.ClientId))
            throw new SharePointConfigurationException("SharePoint:ClientId configuration is required.");

        var hasSecret = !string.IsNullOrWhiteSpace(options.ClientSecret);
        var hasCertificate = !string.IsNullOrWhiteSpace(options.CertificatePath);

        if (!hasSecret && !hasCertificate)
            throw new SharePointConfigurationException(
                "SharePoint authentication requires either ClientSecret or CertificatePath.");

        var hasSiteId = !string.IsNullOrWhiteSpace(options.SiteId);
        var hasSitePath = !string.IsNullOrWhiteSpace(options.SiteHostname)
                          && !string.IsNullOrWhiteSpace(options.SitePath);

        if (!hasSiteId && !hasSitePath)
            throw new SharePointConfigurationException(
                "SharePoint site resolution requires either SiteId or both SiteHostname and SitePath.");
    }
}

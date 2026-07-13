using GovUK.Dfe.CoreLibs.SharePoint;
using GovUK.Dfe.CoreLibs.SharePoint.Exceptions;
using GovUK.Dfe.CoreLibs.SharePoint.Interfaces;
using GovUK.Dfe.CoreLibs.SharePoint.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GovUK.Dfe.CoreLibs.SharePoint.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSharePointServices_WithValidConfiguration_RegistersService()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["SharePoint:TenantId"] = "tenant-id",
            ["SharePoint:ClientId"] = "client-id",
            ["SharePoint:ClientSecret"] = "secret",
            ["SharePoint:SiteId"] = "site-id",
            ["SharePoint:DriveId"] = "drive-id"
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSharePointServices(configuration);

        using var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<ISharePointService>();

        Assert.NotNull(service);
    }

    [Fact]
    public void AddSharePointServices_WithExplicitOptions_RegistersService()
    {
        var options = CreateValidOptions();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSharePointServices(options);

        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<ISharePointService>());
        Assert.Same(options, provider.GetRequiredService<SharePointOptions>());
    }

    [Fact]
    public void AddSharePointServices_WithCertificateAuth_DoesNotThrowOnRegistration()
    {
        var options = CreateValidOptions();
        options.ClientSecret = string.Empty;
        options.CertificatePath = Path.GetTempFileName();

        try
        {
            var services = new ServiceCollection();
            services.AddLogging();

            var exception = Record.Exception(() => services.AddSharePointServices(options));

            Assert.Null(exception);
            Assert.Contains(services, d => d.ServiceType == typeof(ISharePointService));
        }
        finally
        {
            File.Delete(options.CertificatePath);
        }
    }

    [Fact]
    public void AddSharePointServices_MissingTenantId_Throws()
    {
        var options = CreateValidOptions();
        options.TenantId = string.Empty;

        var ex = Assert.Throws<SharePointConfigurationException>(() =>
            new ServiceCollection().AddSharePointServices(options));

        Assert.Contains("TenantId", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddSharePointServices_MissingClientId_Throws()
    {
        var options = CreateValidOptions();
        options.ClientId = string.Empty;

        var ex = Assert.Throws<SharePointConfigurationException>(() =>
            new ServiceCollection().AddSharePointServices(options));

        Assert.Contains("ClientId", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddSharePointServices_MissingAuth_Throws()
    {
        var options = CreateValidOptions();
        options.ClientSecret = string.Empty;
        options.CertificatePath = string.Empty;

        var ex = Assert.Throws<SharePointConfigurationException>(() =>
            new ServiceCollection().AddSharePointServices(options));

        Assert.Contains("ClientSecret", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddSharePointServices_MissingSite_Throws()
    {
        var options = CreateValidOptions();
        options.SiteId = string.Empty;
        options.SiteHostname = string.Empty;
        options.SitePath = string.Empty;

        var ex = Assert.Throws<SharePointConfigurationException>(() =>
            new ServiceCollection().AddSharePointServices(options));

        Assert.Contains("SiteId", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddSharePointServices_MissingDrive_Throws()
    {
        var options = CreateValidOptions();
        options.DriveId = string.Empty;
        options.LibraryName = string.Empty;

        var ex = Assert.Throws<SharePointConfigurationException>(() =>
            new ServiceCollection().AddSharePointServices(options));

        Assert.Contains("DriveId", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddSharePointServices_WithSiteHostnameAndPath_Succeeds()
    {
        var options = CreateValidOptions();
        options.SiteId = string.Empty;
        options.SiteHostname = "contoso.sharepoint.com";
        options.SitePath = "/sites/MySite";

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSharePointServices(options);

        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<ISharePointService>());
    }

    [Fact]
    public void AddSharePointServices_NullServices_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ServiceCollectionExtensions.AddSharePointServices(null!, CreateValidOptions()));
    }

    [Fact]
    public void AddSharePointServices_NullConfiguration_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ServiceCollection().AddSharePointServices((IConfiguration)null!));
    }

    private static SharePointOptions CreateValidOptions() => new()
    {
        TenantId = "tenant-id",
        ClientId = "client-id",
        ClientSecret = "secret",
        SiteId = "site-id",
        DriveId = "drive-id"
    };

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}

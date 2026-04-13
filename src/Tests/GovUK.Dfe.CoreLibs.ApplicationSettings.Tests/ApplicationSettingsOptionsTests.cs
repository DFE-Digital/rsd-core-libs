using FluentAssertions;
using GovUK.Dfe.CoreLibs.ApplicationSettings.Configuration;

namespace GovUK.Dfe.CoreLibs.ApplicationSettings.Tests.Configuration;

public class ApplicationSettingsOptionsTests
{
    [Fact]
    public void ApplicationSettingsOptions_ShouldHaveCorrectDefaultValues()
    {
        // Act
        var options = new ApplicationSettingsOptions();

        // Assert
        options.Schema.Should().BeNull();
        options.EnableCaching.Should().BeTrue();
        options.CacheExpirationMinutes.Should().Be(30);
        options.DefaultCategory.Should().Be("General");
    }

    [Fact]
    public void ApplicationSettingsOptions_ShouldAllowPropertyOverrides()
    {
        // Act
        var options = new ApplicationSettingsOptions
        {
            Schema = "CustomSchema",
            EnableCaching = false,
            CacheExpirationMinutes = 60,
            DefaultCategory = "Custom"
        };

        // Assert
        options.Schema.Should().Be("CustomSchema");
        options.EnableCaching.Should().BeFalse();
        options.CacheExpirationMinutes.Should().Be(60);
        options.DefaultCategory.Should().Be("Custom");

    }

    [Fact]
    public void ApplicationSettingsOptions_ConfigurationSection_ShouldBeCorrect()
    {
        // Assert
        ApplicationSettingsOptions.ConfigurationSection.Should().Be("ApplicationSettings");
    }
}
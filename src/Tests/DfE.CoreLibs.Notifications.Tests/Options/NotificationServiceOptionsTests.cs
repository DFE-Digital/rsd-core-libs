using DfE.CoreLibs.Notifications.Options;

namespace DfE.CoreLibs.Notifications.Tests.Options;

public class NotificationServiceOptionsTests
{
    [Fact]
    public void NotificationServiceOptions_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var options = new NotificationServiceOptions();

        // Assert
        Assert.Equal(NotificationStorageProvider.Session, options.StorageProvider);
        Assert.Equal(50, options.MaxNotificationsPerUser);
        Assert.Equal(60, options.AutoCleanupIntervalMinutes);
        Assert.Equal(24, options.MaxNotificationAgeHours);
        Assert.Null(options.RedisConnectionString);
        Assert.Equal("notifications:", options.RedisKeyPrefix);
        Assert.Equal("UserNotifications", options.SessionKey);
        Assert.NotNull(options.TypeDefaults);
    }

    [Fact]
    public void NotificationServiceOptions_SectionName_IsCorrect()
    {
        // Assert
        Assert.Equal("NotificationService", NotificationServiceOptions.SectionName);
    }

    [Fact]
    public void NotificationServiceOptions_SetProperties_WorksCorrectly()
    {
        // Arrange
        var options = new NotificationServiceOptions();
        var typeDefaults = new NotificationTypeDefaults();

        // Act
        options.StorageProvider = NotificationStorageProvider.Redis;
        options.MaxNotificationsPerUser = 100;
        options.AutoCleanupIntervalMinutes = 120;
        options.MaxNotificationAgeHours = 48;
        options.RedisConnectionString = "localhost:6379";
        options.RedisKeyPrefix = "test:";
        options.SessionKey = "TestNotifications";
        options.TypeDefaults = typeDefaults;

        // Assert
        Assert.Equal(NotificationStorageProvider.Redis, options.StorageProvider);
        Assert.Equal(100, options.MaxNotificationsPerUser);
        Assert.Equal(120, options.AutoCleanupIntervalMinutes);
        Assert.Equal(48, options.MaxNotificationAgeHours);
        Assert.Equal("localhost:6379", options.RedisConnectionString);
        Assert.Equal("test:", options.RedisKeyPrefix);
        Assert.Equal("TestNotifications", options.SessionKey);
        Assert.Same(typeDefaults, options.TypeDefaults);
    }
}

public class NotificationStorageProviderTests
{
    [Fact]
    public void NotificationStorageProvider_HasExpectedValues()
    {
        // Assert
        Assert.Equal(0, (int)NotificationStorageProvider.Session);
        Assert.Equal(1, (int)NotificationStorageProvider.Redis);
        Assert.Equal(2, (int)NotificationStorageProvider.InMemory);
    }
}

public class NotificationTypeDefaultsTests
{
    [Fact]
    public void NotificationTypeDefaults_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var defaults = new NotificationTypeDefaults();

        // Assert
        Assert.NotNull(defaults.Success);
        Assert.True(defaults.Success.AutoDismiss);
        Assert.Equal(5, defaults.Success.AutoDismissSeconds);

        Assert.NotNull(defaults.Error);
        Assert.False(defaults.Error.AutoDismiss);
        Assert.Equal(10, defaults.Error.AutoDismissSeconds);

        Assert.NotNull(defaults.Info);
        Assert.True(defaults.Info.AutoDismiss);
        Assert.Equal(5, defaults.Info.AutoDismissSeconds);

        Assert.NotNull(defaults.Warning);
        Assert.True(defaults.Warning.AutoDismiss);
        Assert.Equal(7, defaults.Warning.AutoDismissSeconds);
    }

    [Fact]
    public void NotificationTypeDefaults_SetProperties_WorksCorrectly()
    {
        // Arrange
        var defaults = new NotificationTypeDefaults();
        var successSettings = new NotificationTypeSettings { AutoDismiss = false, AutoDismissSeconds = 15 };
        var errorSettings = new NotificationTypeSettings { AutoDismiss = true, AutoDismissSeconds = 20 };
        var infoSettings = new NotificationTypeSettings { AutoDismiss = false, AutoDismissSeconds = 25 };
        var warningSettings = new NotificationTypeSettings { AutoDismiss = true, AutoDismissSeconds = 30 };

        // Act
        defaults.Success = successSettings;
        defaults.Error = errorSettings;
        defaults.Info = infoSettings;
        defaults.Warning = warningSettings;

        // Assert
        Assert.Same(successSettings, defaults.Success);
        Assert.Same(errorSettings, defaults.Error);
        Assert.Same(infoSettings, defaults.Info);
        Assert.Same(warningSettings, defaults.Warning);
    }
}

public class NotificationTypeSettingsTests
{
    [Fact]
    public void NotificationTypeSettings_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var settings = new NotificationTypeSettings();

        // Assert
        Assert.True(settings.AutoDismiss);
        Assert.Equal(5, settings.AutoDismissSeconds);
    }

    [Fact]
    public void NotificationTypeSettings_SetProperties_WorksCorrectly()
    {
        // Arrange
        var settings = new NotificationTypeSettings();

        // Act
        settings.AutoDismiss = false;
        settings.AutoDismissSeconds = 30;

        // Assert
        Assert.False(settings.AutoDismiss);
        Assert.Equal(30, settings.AutoDismissSeconds);
    }
}
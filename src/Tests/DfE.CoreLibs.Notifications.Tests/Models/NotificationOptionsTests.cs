using DfE.CoreLibs.Notifications.Models;

namespace DfE.CoreLibs.Notifications.Tests.Models;

public class NotificationOptionsTests
{
    [Fact]
    public void NotificationOptions_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var options = new NotificationOptions();

        // Assert
        Assert.Null(options.Context);
        Assert.Null(options.Category);
        Assert.True(options.AutoDismiss);
        Assert.Equal(5, options.AutoDismissSeconds);
        Assert.Null(options.UserId);
        Assert.Null(options.ActionUrl);
        Assert.Null(options.Metadata);
        Assert.Equal(NotificationPriority.Normal, options.Priority);
        Assert.True(options.ReplaceExistingContext);
    }

    [Fact]
    public void NotificationOptions_SetProperties_WorksCorrectly()
    {
        // Arrange
        var options = new NotificationOptions();
        var metadata = new Dictionary<string, object> { ["key"] = "value" };

        // Act
        options.Context = "test-context";
        options.Category = "test-category";
        options.AutoDismiss = false;
        options.AutoDismissSeconds = 10;
        options.UserId = "test-user";
        options.ActionUrl = "/test/url";
        options.Metadata = metadata;
        options.Priority = NotificationPriority.Critical;
        options.ReplaceExistingContext = false;

        // Assert
        Assert.Equal("test-context", options.Context);
        Assert.Equal("test-category", options.Category);
        Assert.False(options.AutoDismiss);
        Assert.Equal(10, options.AutoDismissSeconds);
        Assert.Equal("test-user", options.UserId);
        Assert.Equal("/test/url", options.ActionUrl);
        Assert.Same(metadata, options.Metadata);
        Assert.Equal(NotificationPriority.Critical, options.Priority);
        Assert.False(options.ReplaceExistingContext);
    }
}
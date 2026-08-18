using GovUK.Dfe.CoreLibs.Notifications;
using GovUK.Dfe.CoreLibs.Notifications.Models;
using GovUK.Dfe.CoreLibs.Notifications.Options;
using GovUK.Dfe.CoreLibs.Notifications.Storage;
using Xunit;

namespace GovUK.Dfe.CoreLibs.Notifications.Tests;

public class NotificationContextHelperTests
{
    [Theory]
    [InlineData("Transfers", "Transfers", true)]
    [InlineData("Transfers|file-upload|abc", "Transfers", true)]
    [InlineData("Other|file-upload|abc", "Transfers", false)]
    [InlineData("TransfersExtra", "Transfers", false)]
    [InlineData(null, "Transfers", false)]
    [InlineData("Transfers", null, true)]
    public void BelongsToScope_MatchesExactOrPrefix(string? notificationContext, string? scopeContext, bool expected)
    {
        Assert.Equal(expected, NotificationContextHelper.BelongsToScope(notificationContext, scopeContext));
    }

    [Fact]
    public void BuildScopedContext_JoinsNonEmptyParts()
    {
        var context = NotificationContextHelper.BuildScopedContext("Transfers", "file-upload", "abc-123");

        Assert.Equal("Transfers|file-upload|abc-123", context);
    }
}

public class InMemoryNotificationStorageContextTests
{
    [Fact]
    public async Task StoreNotificationAsync_WhenReplaceExistingContextFalse_KeepsMultipleSameScope()
    {
        var storage = new InMemoryNotificationStorage(Microsoft.Extensions.Options.Options.Create(new NotificationServiceOptions()));

        await storage.StoreNotificationAsync(new Notification
        {
            Id = "1",
            UserId = "user1",
            Context = "Transfers|file-upload|file-a",
            ReplaceExistingContext = false
        });
        await storage.StoreNotificationAsync(new Notification
        {
            Id = "2",
            UserId = "user1",
            Context = "Transfers|file-upload|file-b",
            ReplaceExistingContext = false
        });

        var notifications = (await storage.GetNotificationsAsync("user1")).ToList();

        Assert.Equal(2, notifications.Count);
    }

    [Fact]
    public async Task StoreNotificationAsync_WhenReplaceExistingContextTrue_ReplacesSameContextOnly()
    {
        var storage = new InMemoryNotificationStorage(Microsoft.Extensions.Options.Options.Create(new NotificationServiceOptions()));

        await storage.StoreNotificationAsync(new Notification
        {
            Id = "1",
            UserId = "user1",
            Message = "old",
            Context = "Transfers|file-validation|file-a",
            ReplaceExistingContext = true
        });
        await storage.StoreNotificationAsync(new Notification
        {
            Id = "2",
            UserId = "user1",
            Message = "new",
            Context = "Transfers|file-validation|file-a",
            ReplaceExistingContext = true
        });
        await storage.StoreNotificationAsync(new Notification
        {
            Id = "3",
            UserId = "user1",
            Message = "other file",
            Context = "Transfers|file-validation|file-b",
            ReplaceExistingContext = true
        });

        var notifications = (await storage.GetNotificationsAsync("user1")).ToList();

        Assert.Equal(2, notifications.Count);
        Assert.Contains(notifications, n => n.Id == "2" && n.Message == "new");
        Assert.Contains(notifications, n => n.Id == "3");
        Assert.DoesNotContain(notifications, n => n.Id == "1");
    }
}

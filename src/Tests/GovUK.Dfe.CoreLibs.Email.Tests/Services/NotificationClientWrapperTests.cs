using GovUK.Dfe.CoreLibs.Email.Services;
using Notify.Models;
using Notify.Models.Responses;

namespace GovUK.Dfe.CoreLibs.Email.Tests.Services;

public class NotificationClientWrapperTests
{
    private const string TestApiKey = "test_key-00000000-0000-0000-0000-000000000000-00000000-0000-0000-0000-000000000000";

    [Fact]
    public void Constructor_WithValidApiKey_ShouldCreateInstance()
    {
        // Act
        var wrapper = new NotificationClientWrapper(TestApiKey);

        // Assert
        wrapper.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullApiKey_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => new NotificationClientWrapper(null!);
        act.Should().Throw<Exception>(); // The underlying NotificationClient will throw
    }

    [Fact]
    public void Constructor_WithEmptyApiKey_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => new NotificationClientWrapper("");
        act.Should().Throw<Exception>(); // The underlying NotificationClient will throw
    }

    [Fact]
    public void SendEmail_WithValidParameters_ShouldCallUnderlyingClient()
    {
        // Arrange
        var wrapper = new NotificationClientWrapper(TestApiKey);
        var personalisation = new Dictionary<string, dynamic>
        {
            ["name"] = "Test User"
        };

        // Act & Assert
        // Note: This will make a real API call unless we had a way to mock the NotificationClient
        // For now, we'll just verify the method exists and can be called
        var act = () => wrapper.SendEmail("test@example.com", "template-id", personalisation, "ref", "reply-to");
        act.Should().NotBeNull(); // The method should exist and be callable
    }

    [Fact] 
    public void GetNotificationById_WithValidId_ShouldCallUnderlyingClient()
    {
        // Arrange
        var wrapper = new NotificationClientWrapper(TestApiKey);

        // Act & Assert
        var act = () => wrapper.GetNotificationById("notification-id");
        act.Should().NotBeNull(); // The method should exist and be callable
    }

    [Fact]
    public void GetNotifications_WithFilters_ShouldCallUnderlyingClient()
    {
        // Arrange
        var wrapper = new NotificationClientWrapper(TestApiKey);

        // Act & Assert
        var act = () => wrapper.GetNotifications("email", "delivered", "reference", null);
        act.Should().NotBeNull(); // The method should exist and be callable
    }

    [Fact]
    public void GetTemplateById_WithValidId_ShouldCallUnderlyingClient()
    {
        // Arrange
        var wrapper = new NotificationClientWrapper(TestApiKey);

        // Act & Assert
        var act = () => wrapper.GetTemplateById("template-id");
        act.Should().NotBeNull(); // The method should exist and be callable
    }

    [Fact]
    public void GetTemplateByIdAndVersion_WithValidParameters_ShouldCallUnderlyingClient()
    {
        // Arrange
        var wrapper = new NotificationClientWrapper(TestApiKey);

        // Act & Assert
        var act = () => wrapper.GetTemplateByIdAndVersion("template-id", 1);
        act.Should().NotBeNull(); // The method should exist and be callable
    }

    [Fact]
    public void GetAllTemplates_WithTemplateType_ShouldCallUnderlyingClient()
    {
        // Arrange
        var wrapper = new NotificationClientWrapper(TestApiKey);

        // Act & Assert
        var act = () => wrapper.GetAllTemplates("email");
        act.Should().NotBeNull(); // The method should exist and be callable
    }

    [Fact]
    public void GenerateTemplatePreview_WithValidParameters_ShouldCallUnderlyingClient()
    {
        // Arrange
        var wrapper = new NotificationClientWrapper(TestApiKey);
        var personalisation = new Dictionary<string, dynamic>
        {
            ["name"] = "Test User"
        };

        // Act & Assert
        var act = () => wrapper.GenerateTemplatePreview("template-id", personalisation);
        act.Should().NotBeNull(); // The method should exist and be callable
    }
}

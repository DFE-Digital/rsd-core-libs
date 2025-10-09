using GovUK.Dfe.CoreLibs.Email.Models;

namespace GovUK.Dfe.CoreLibs.Email.Tests.Models;

public class EmailMessageTests
{
    [Fact]
    public void GetAllRecipients_WithOnlyToEmail_ShouldReturnSingleRecipient()
    {
        // Arrange
        var emailMessage = new EmailMessage
        {
            ToEmail = "test@example.com"
        };

        // Act
        var recipients = emailMessage.GetAllRecipients();

        // Assert
        recipients.Should().HaveCount(1);
        recipients.Should().Contain("test@example.com");
    }

    [Fact]
    public void GetAllRecipients_WithOnlyToEmails_ShouldReturnAllRecipients()
    {
        // Arrange
        var emailMessage = new EmailMessage
        {
            ToEmails = new List<string> { "test1@example.com", "test2@example.com", "test3@example.com" }
        };

        // Act
        var recipients = emailMessage.GetAllRecipients();

        // Assert
        recipients.Should().HaveCount(3);
        recipients.Should().Contain("test1@example.com");
        recipients.Should().Contain("test2@example.com");
        recipients.Should().Contain("test3@example.com");
    }

    [Fact]
    public void GetAllRecipients_WithBothToEmailAndToEmails_ShouldReturnAllRecipients()
    {
        // Arrange
        var emailMessage = new EmailMessage
        {
            ToEmail = "primary@example.com",
            ToEmails = new List<string> { "secondary1@example.com", "secondary2@example.com" }
        };

        // Act
        var recipients = emailMessage.GetAllRecipients();

        // Assert
        recipients.Should().HaveCount(3);
        recipients.Should().Contain("primary@example.com");
        recipients.Should().Contain("secondary1@example.com");
        recipients.Should().Contain("secondary2@example.com");
    }

    [Fact]
    public void GetAllRecipients_WithDuplicateEmails_ShouldReturnDistinctRecipients()
    {
        // Arrange
        var emailMessage = new EmailMessage
        {
            ToEmail = "test@example.com",
            ToEmails = new List<string> { "test@example.com", "other@example.com" }
        };

        // Act
        var recipients = emailMessage.GetAllRecipients();

        // Assert
        recipients.Should().HaveCount(2);
        recipients.Should().Contain("test@example.com");
        recipients.Should().Contain("other@example.com");
    }

    [Fact]
    public void GetAllRecipients_WithEmptyAndWhitespaceEmails_ShouldFilterThem()
    {
        // Arrange
        var emailMessage = new EmailMessage
        {
            ToEmail = "  ",
            ToEmails = new List<string> { "valid@example.com", "", " ", "another@example.com" }
        };

        // Act
        var recipients = emailMessage.GetAllRecipients();

        // Assert
        recipients.Should().HaveCount(2);
        recipients.Should().Contain("valid@example.com");
        recipients.Should().Contain("another@example.com");
    }

    [Fact]
    public void GetAllRecipients_WithNoRecipients_ShouldReturnEmptyList()
    {
        // Arrange
        var emailMessage = new EmailMessage();

        // Act
        var recipients = emailMessage.GetAllRecipients();

        // Assert
        recipients.Should().BeEmpty();
    }

    [Fact]
    public void GetPrimaryRecipient_WithToEmail_ShouldReturnToEmail()
    {
        // Arrange
        var emailMessage = new EmailMessage
        {
            ToEmail = "primary@example.com",
            ToEmails = new List<string> { "secondary@example.com" }
        };

        // Act
        var primary = emailMessage.GetPrimaryRecipient();

        // Assert
        primary.Should().Be("primary@example.com");
    }

    [Fact]
    public void GetPrimaryRecipient_WithoutToEmailButWithToEmails_ShouldReturnFirstFromToEmails()
    {
        // Arrange
        var emailMessage = new EmailMessage
        {
            ToEmails = new List<string> { "first@example.com", "second@example.com" }
        };

        // Act
        var primary = emailMessage.GetPrimaryRecipient();

        // Assert
        primary.Should().Be("first@example.com");
    }

    [Fact]
    public void GetPrimaryRecipient_WithEmptyToEmailButValidToEmails_ShouldReturnFirstValidFromToEmails()
    {
        // Arrange
        var emailMessage = new EmailMessage
        {
            ToEmail = "  ",
            ToEmails = new List<string> { "", "valid@example.com", "another@example.com" }
        };

        // Act
        var primary = emailMessage.GetPrimaryRecipient();

        // Assert
        primary.Should().Be("valid@example.com");
    }

    [Fact]
    public void GetPrimaryRecipient_WithNoValidRecipients_ShouldReturnNull()
    {
        // Arrange
        var emailMessage = new EmailMessage
        {
            ToEmail = "  ",
            ToEmails = new List<string> { "", " " }
        };

        // Act
        var primary = emailMessage.GetPrimaryRecipient();

        // Assert
        primary.Should().BeNull();
    }

    [Fact]
    public void GetPrimaryRecipient_WithNoRecipients_ShouldReturnNull()
    {
        // Arrange
        var emailMessage = new EmailMessage();

        // Act
        var primary = emailMessage.GetPrimaryRecipient();

        // Assert
        primary.Should().BeNull();
    }
}

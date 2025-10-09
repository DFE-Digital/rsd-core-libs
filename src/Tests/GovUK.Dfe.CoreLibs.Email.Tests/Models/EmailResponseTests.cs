using GovUK.Dfe.CoreLibs.Email.Models;

namespace GovUK.Dfe.CoreLibs.Email.Tests.Models;

public class EmailResponseTests
{
    [Fact]
    public void EmailResponse_WithRequiredProperties_ShouldBeValid()
    {
        // Arrange & Act
        var response = new EmailResponse
        {
            Id = "test-id-123",
            Status = EmailStatus.Sent,
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        response.Id.Should().Be("test-id-123");
        response.Status.Should().Be(EmailStatus.Sent);
        response.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        response.Reference.Should().BeNull();
        response.Uri.Should().BeNull();
        response.Template.Should().BeNull();
        response.Content.Should().BeNull();
        response.SentAt.Should().BeNull();
        response.CompletedAt.Should().BeNull();
        response.Metadata.Should().BeNull();
        response.Recipients.Should().BeNull();
        response.RecipientResponses.Should().BeNull();
    }

    [Fact]
    public void EmailResponse_WithAllProperties_ShouldSetAllValues()
    {
        // Arrange
        var template = new EmailTemplate { Id = "template-1", Version = 1 };
        var content = new EmailContent { Subject = "Test", Body = "Test body" };
        var metadata = new Dictionary<string, object> { ["key1"] = "value1" };
        var recipients = new List<string> { "test1@example.com", "test2@example.com" };
        var recipientResponses = new List<EmailResponse>
        {
            new() { Id = "resp1", Status = EmailStatus.Delivered, CreatedAt = DateTime.UtcNow },
            new() { Id = "resp2", Status = EmailStatus.Sent, CreatedAt = DateTime.UtcNow }
        };

        // Act
        var response = new EmailResponse
        {
            Id = "main-id",
            Reference = "ref-123",
            Uri = "https://api.example.com/emails/main-id",
            Status = EmailStatus.Delivered,
            Template = template,
            Content = content,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            SentAt = DateTime.UtcNow.AddMinutes(-4),
            CompletedAt = DateTime.UtcNow.AddMinutes(-3),
            Metadata = metadata,
            Recipients = recipients,
            RecipientResponses = recipientResponses
        };

        // Assert
        response.Id.Should().Be("main-id");
        response.Reference.Should().Be("ref-123");
        response.Uri.Should().Be("https://api.example.com/emails/main-id");
        response.Status.Should().Be(EmailStatus.Delivered);
        response.Template.Should().BeSameAs(template);
        response.Content.Should().BeSameAs(content);
        response.CreatedAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(-5), TimeSpan.FromSeconds(1));
        response.SentAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(-4), TimeSpan.FromSeconds(1));
        response.CompletedAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(-3), TimeSpan.FromSeconds(1));
        response.Metadata.Should().BeSameAs(metadata);
        response.Recipients.Should().BeSameAs(recipients);
        response.RecipientResponses.Should().BeSameAs(recipientResponses);
    }
}

using GovUK.Dfe.CoreLibs.Email.Models;

namespace GovUK.Dfe.CoreLibs.Email.Tests.Models;

public class EmailContentTests
{
    private readonly IFixture _fixture;

    public EmailContentTests()
    {
        _fixture = new Fixture();
    }

    [Fact]
    public void EmailContent_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var content = new EmailContent();

        // Assert
        content.FromEmail.Should().BeNull();
        content.Body.Should().BeNull();
        content.Subject.Should().BeNull();
    }

    [Fact]
    public void EmailContent_WithAllProperties_ShouldSetAllValues()
    {
        // Arrange
        var fromEmail = _fixture.Create<string>();
        var body = _fixture.Create<string>();
        var subject = _fixture.Create<string>();

        // Act
        var content = new EmailContent
        {
            FromEmail = fromEmail,
            Body = body,
            Subject = subject
        };

        // Assert
        content.FromEmail.Should().Be(fromEmail);
        content.Body.Should().Be(body);
        content.Subject.Should().Be(subject);
    }

    [Fact]
    public void EmailContent_WithTypicalEmailContent_ShouldBeValid()
    {
        // Arrange & Act
        var content = new EmailContent
        {
            FromEmail = "noreply@example.com",
            Body = "Dear Customer, thank you for your order.",
            Subject = "Order Confirmation"
        };

        // Assert
        content.Should().NotBeNull();
        content.FromEmail.Should().Be("noreply@example.com");
        content.Body.Should().Be("Dear Customer, thank you for your order.");
        content.Subject.Should().Be("Order Confirmation");
    }
}

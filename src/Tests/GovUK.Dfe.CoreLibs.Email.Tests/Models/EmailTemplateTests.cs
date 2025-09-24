using GovUK.Dfe.CoreLibs.Email.Models;

namespace GovUK.Dfe.CoreLibs.Email.Tests.Models;

public class EmailTemplateTests
{
    private readonly IFixture _fixture;

    public EmailTemplateTests()
    {
        _fixture = new Fixture();
    }

    [Fact]
    public void EmailTemplate_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var template = new EmailTemplate { Id = "test-id" };

        // Assert
        template.Id.Should().Be("test-id");
        template.Name.Should().BeNull();
        template.Type.Should().BeNull();
        template.Version.Should().Be(0);
        template.Body.Should().BeNull();
        template.Subject.Should().BeNull();
        template.Uri.Should().BeNull();
    }

    [Fact]
    public void EmailTemplate_WithAllProperties_ShouldSetAllValues()
    {
        // Arrange
        var id = _fixture.Create<string>();
        var name = _fixture.Create<string>();
        var type = _fixture.Create<string>();
        var version = _fixture.Create<int>();
        var body = _fixture.Create<string>();
        var subject = _fixture.Create<string>();
        var uri = _fixture.Create<string>();

        // Act
        var template = new EmailTemplate
        {
            Id = id,
            Name = name,
            Type = type,
            Version = version,
            Body = body,
            Subject = subject,
            Uri = uri
        };

        // Assert
        template.Id.Should().Be(id);
        template.Name.Should().Be(name);
        template.Type.Should().Be(type);
        template.Version.Should().Be(version);
        template.Body.Should().Be(body);
        template.Subject.Should().Be(subject);
        template.Uri.Should().Be(uri);
    }

    [Fact]
    public void EmailTemplate_WithRequiredProperties_ShouldBeValid()
    {
        // Arrange & Act
        var template = new EmailTemplate
        {
            Id = "template-123",
            Name = "Welcome Email",
            Type = "email",
            Version = 2,
            Body = "Hello {{name}}, welcome to our service!",
            Subject = "Welcome {{name}}"
        };

        // Assert
        template.Should().NotBeNull();
        template.Id.Should().Be("template-123");
        template.Name.Should().Be("Welcome Email");
        template.Type.Should().Be("email");
        template.Version.Should().Be(2);
        template.Body.Should().Be("Hello {{name}}, welcome to our service!");
        template.Subject.Should().Be("Welcome {{name}}");
    }
}

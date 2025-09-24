using GovUK.Dfe.CoreLibs.Email.Models;

namespace GovUK.Dfe.CoreLibs.Email.Tests.Models;

public class TemplatePreviewTests
{
    private readonly IFixture _fixture;

    public TemplatePreviewTests()
    {
        _fixture = new Fixture();
    }

    [Fact]
    public void TemplatePreview_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var templatePreview = new TemplatePreview { Id = "test-id" };

        // Assert
        templatePreview.Id.Should().Be("test-id");
        templatePreview.Type.Should().BeNull();
        templatePreview.Version.Should().Be(0);
        templatePreview.Body.Should().BeNull();
        templatePreview.Subject.Should().BeNull();
        templatePreview.Html.Should().BeNull();
    }

    [Fact]
    public void TemplatePreview_WithAllProperties_ShouldSetAllValues()
    {
        // Arrange
        var id = _fixture.Create<string>();
        var type = _fixture.Create<string>();
        var version = _fixture.Create<int>();
        var body = _fixture.Create<string>();
        var subject = _fixture.Create<string>();
        var html = _fixture.Create<string>();

        // Act
        var templatePreview = new TemplatePreview
        {
            Id = id,
            Type = type,
            Version = version,
            Body = body,
            Subject = subject,
            Html = html
        };

        // Assert
        templatePreview.Id.Should().Be(id);
        templatePreview.Type.Should().Be(type);
        templatePreview.Version.Should().Be(version);
        templatePreview.Body.Should().Be(body);
        templatePreview.Subject.Should().Be(subject);
        templatePreview.Html.Should().Be(html);
    }

    [Fact]
    public void TemplatePreview_WithRequiredProperties_ShouldBeValid()
    {
        // Arrange & Act
        var templatePreview = new TemplatePreview
        {
            Id = "template-123",
            Type = "email",
            Version = 1,
            Body = "Hello {{name}}",
            Subject = "Welcome"
        };

        // Assert
        templatePreview.Should().NotBeNull();
        templatePreview.Id.Should().Be("template-123");
        templatePreview.Type.Should().Be("email");
        templatePreview.Version.Should().Be(1);
        templatePreview.Body.Should().Be("Hello {{name}}");
        templatePreview.Subject.Should().Be("Welcome");
    }
}

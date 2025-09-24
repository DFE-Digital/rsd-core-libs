using GovUK.Dfe.CoreLibs.Email.Models;

namespace GovUK.Dfe.CoreLibs.Email.Tests.Models;

public class EmailAttachmentTests
{
    [Fact]
    public void EmailAttachment_WithRequiredProperties_ShouldBeValid()
    {
        // Arrange & Act
        var attachment = new EmailAttachment
        {
            FileName = "test.pdf",
            Content = new byte[] { 1, 2, 3, 4, 5 }
        };

        // Assert
        attachment.FileName.Should().Be("test.pdf");
        attachment.Content.Should().Equal(new byte[] { 1, 2, 3, 4, 5 });
        attachment.ContentType.Should().BeNull();
        attachment.IsInline.Should().BeFalse();
        attachment.ContentId.Should().BeNull();
    }

    [Fact]
    public void EmailAttachment_WithAllProperties_ShouldSetAllValues()
    {
        // Arrange & Act
        var attachment = new EmailAttachment
        {
            FileName = "image.png",
            Content = new byte[] { 10, 20, 30 },
            ContentType = "image/png",
            IsInline = true,
            ContentId = "img001"
        };

        // Assert
        attachment.FileName.Should().Be("image.png");
        attachment.Content.Should().Equal(new byte[] { 10, 20, 30 });
        attachment.ContentType.Should().Be("image/png");
        attachment.IsInline.Should().BeTrue();
        attachment.ContentId.Should().Be("img001");
    }

    [Fact]
    public void EmailAttachment_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var attachment = new EmailAttachment
        {
            FileName = "test.txt",
            Content = Array.Empty<byte>()
        };

        // Assert
        attachment.IsInline.Should().BeFalse();
        attachment.ContentType.Should().BeNull();
        attachment.ContentId.Should().BeNull();
    }
}

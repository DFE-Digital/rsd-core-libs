using FluentAssertions;
using GovUK.Dfe.CoreLibs.Http.NoScriptDetection.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace GovUK.Dfe.CoreLibs.Http.Tests.NoScriptDetection
{
    public class NoScriptDetectionTagHelperTests
    {
        private readonly NoScriptDetectionTagHelper _tagHelper;

        public NoScriptDetectionTagHelperTests()
        {
            _tagHelper = new NoScriptDetectionTagHelper();
        }

        [Fact]
        public void Process_ShouldSetTagNameToNoscript()
        {
            // Arrange
            var context = CreateTagHelperContext();
            var output = CreateTagHelperOutput("noscript-detection");

            // Act
            _tagHelper.Process(context, output);

            // Assert
            output.TagName.Should().Be("noscript");
        }

        [Fact]
        public void Process_ShouldSetHtmlContent()
        {
            // Arrange
            var context = CreateTagHelperContext();
            var output = CreateTagHelperOutput("noscript-detection");

            // Act
            _tagHelper.Process(context, output);

            // Assert
            output.Content.GetContent().Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void Process_ShouldContainImgElement()
        {
            // Arrange
            var context = CreateTagHelperContext();
            var output = CreateTagHelperOutput("noscript-detection");

            // Act
            _tagHelper.Process(context, output);

            // Assert
            var content = output.Content.GetContent();
            content.Should().Contain("<img");
            content.Should().Contain("/>");
        }

        [Fact]
        public void Process_ShouldSetCorrectImageSource()
        {
            // Arrange
            var context = CreateTagHelperContext();
            var output = CreateTagHelperOutput("noscript-detection");

            // Act
            _tagHelper.Process(context, output);

            // Assert
            var content = output.Content.GetContent();
            content.Should().Contain("src=\"/_noscript/pixel\"");
        }

        [Fact]
        public void Process_ShouldSetEmptyAltAttribute()
        {
            // Arrange
            var context = CreateTagHelperContext();
            var output = CreateTagHelperOutput("noscript-detection");

            // Act
            _tagHelper.Process(context, output);

            // Assert
            var content = output.Content.GetContent();
            content.Should().Contain("alt=\"\"");
        }

        [Fact]
        public void Process_ShouldSetImageDimensions()
        {
            // Arrange
            var context = CreateTagHelperContext();
            var output = CreateTagHelperOutput("noscript-detection");

            // Act
            _tagHelper.Process(context, output);

            // Assert
            var content = output.Content.GetContent();
            content.Should().Contain("width=\"1\"");
            content.Should().Contain("height=\"1\"");
        }

        [Fact]
        public void Process_ShouldHideImageWithStyle()
        {
            // Arrange
            var context = CreateTagHelperContext();
            var output = CreateTagHelperOutput("noscript-detection");

            // Act
            _tagHelper.Process(context, output);

            // Assert
            var content = output.Content.GetContent();
            content.Should().Contain("style=\"display:none\"");
        }

        [Fact]
        public void TagHelper_ShouldHaveHtmlTargetElementAttribute()
        {
            // Assert
            var attribute = typeof(NoScriptDetectionTagHelper)
                .GetCustomAttributes(typeof(HtmlTargetElementAttribute), false)
                .FirstOrDefault() as HtmlTargetElementAttribute;

            attribute.Should().NotBeNull();
            attribute!.Tag.Should().Be("noscript-detection");
        }

        [Fact]
        public void TagHelper_ShouldInheritFromTagHelper()
        {
            // Assert
            _tagHelper.Should().BeAssignableTo<TagHelper>();
        }

        [Fact]
        public void Process_ShouldGenerateCompleteImgTag()
        {
            // Arrange
            var context = CreateTagHelperContext();
            var output = CreateTagHelperOutput("noscript-detection");

            // Act
            _tagHelper.Process(context, output);

            // Assert
            var content = output.Content.GetContent();
            
            // Verify complete structure
            content.Should().Contain("src=\"/_noscript/pixel\"");
            content.Should().Contain("alt=\"\"");
            content.Should().Contain("width=\"1\"");
            content.Should().Contain("height=\"1\"");
            content.Should().Contain("style=\"display:none\"");
        }

        [Fact]
        public void Process_ShouldNotModifyAttributes()
        {
            // Arrange
            var context = CreateTagHelperContext();
            var output = CreateTagHelperOutput("noscript-detection");
            var originalAttributeCount = output.Attributes.Count;

            // Act
            _tagHelper.Process(context, output);

            // Assert
            output.Attributes.Count.Should().Be(originalAttributeCount);
        }

        private static TagHelperContext CreateTagHelperContext()
        {
            return new TagHelperContext(
                tagName: "noscript-detection",
                allAttributes: new TagHelperAttributeList(),
                items: new Dictionary<object, object>(),
                uniqueId: Guid.NewGuid().ToString());
        }

        private static TagHelperOutput CreateTagHelperOutput(string tagName)
        {
            return new TagHelperOutput(
                tagName: tagName,
                attributes: new TagHelperAttributeList(),
                getChildContentAsync: (useCachedResult, encoder) =>
                    Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
        }
    }
}


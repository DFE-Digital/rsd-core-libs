using FluentAssertions;
using GovUK.Dfe.CoreLibs.Http.NoScriptDetection.Pixel;

namespace GovUK.Dfe.CoreLibs.Http.Tests.NoScriptDetection
{
    public class TransparentPixelProviderTests
    {
        private readonly TransparentPixelProvider _provider;

        public TransparentPixelProviderTests()
        {
            _provider = new TransparentPixelProvider();
        }

        [Fact]
        public void GetPixel_ShouldReturnNonEmptyByteArray()
        {
            // Act
            var result = _provider.GetPixel();

            // Assert
            result.Should().NotBeNull();
            result.Should().NotBeEmpty();
        }

        [Fact]
        public void GetPixel_ShouldReturnValidPngData()
        {
            // Act
            var result = _provider.GetPixel();

            // Assert
            // PNG files start with these magic bytes: 0x89 0x50 0x4E 0x47
            result.Length.Should().BeGreaterThan(4);
            result[0].Should().Be(0x89);
            result[1].Should().Be(0x50); // 'P'
            result[2].Should().Be(0x4E); // 'N'
            result[3].Should().Be(0x47); // 'G'
        }

        [Fact]
        public void GetPixel_ShouldReturnSameInstanceEachTime()
        {
            // Act
            var result1 = _provider.GetPixel();
            var result2 = _provider.GetPixel();

            // Assert
            result1.Should().BeSameAs(result2);
        }

        [Fact]
        public void GetPixel_ShouldReturn1x1TransparentPng()
        {
            // Arrange
            // Expected base64 encoded 1x1 transparent PNG
            var expectedBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO0pF1sAAAAASUVORK5CYII=";
            var expectedBytes = Convert.FromBase64String(expectedBase64);

            // Act
            var result = _provider.GetPixel();

            // Assert
            result.Should().BeEquivalentTo(expectedBytes);
        }

        [Fact]
        public void GetPixel_ShouldImplementINoScriptPixelProvider()
        {
            // Assert
            _provider.Should().BeAssignableTo<INoScriptPixelProvider>();
        }

        [Fact]
        public void GetPixel_MultipleInstances_ShouldReturnEquivalentData()
        {
            // Arrange
            var provider1 = new TransparentPixelProvider();
            var provider2 = new TransparentPixelProvider();

            // Act
            var result1 = provider1.GetPixel();
            var result2 = provider2.GetPixel();

            // Assert
            result1.Should().BeEquivalentTo(result2);
        }
    }
}


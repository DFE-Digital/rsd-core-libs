using GovUK.Dfe.CoreLibs.AsyncProcessing.Configurations;

namespace GovUK.Dfe.CoreLibs.AsyncProcessing.Tests.Configurations
{
    public class BackgroundServiceOptionsTests
    {
        [Fact]
        public void BackgroundServiceOptions_ShouldHaveCorrectDefaults()
        {
            // Arrange & Act
            var options = new BackgroundServiceOptions();

            // Assert
            Assert.False(options.UseGlobalStoppingToken);
            Assert.Equal(1, options.MaxConcurrentWorkers);
            Assert.Equal(int.MaxValue, options.ChannelCapacity);
            Assert.Equal(ChannelFullMode.Wait, options.ChannelFullMode);
            Assert.False(options.EnableDetailedLogging);
        }

        [Fact]
        public void BackgroundServiceOptions_ShouldAllowSettingUseGlobalStoppingToken()
        {
            // Arrange
            var options = new BackgroundServiceOptions();

            // Act
            options.UseGlobalStoppingToken = true;

            // Assert
            Assert.True(options.UseGlobalStoppingToken);
        }

        [Fact]
        public void BackgroundServiceOptions_ShouldAllowSettingMaxConcurrentWorkers()
        {
            // Arrange
            var options = new BackgroundServiceOptions();

            // Act
            options.MaxConcurrentWorkers = 10;

            // Assert
            Assert.Equal(10, options.MaxConcurrentWorkers);
        }

        [Fact]
        public void BackgroundServiceOptions_ShouldAllowSettingChannelCapacity()
        {
            // Arrange
            var options = new BackgroundServiceOptions();

            // Act
            options.ChannelCapacity = 500;

            // Assert
            Assert.Equal(500, options.ChannelCapacity);
        }

        [Fact]
        public void BackgroundServiceOptions_ShouldAllowSettingChannelFullMode()
        {
            // Arrange
            var options = new BackgroundServiceOptions();

            // Act
            options.ChannelFullMode = ChannelFullMode.DropOldest;

            // Assert
            Assert.Equal(ChannelFullMode.DropOldest, options.ChannelFullMode);
        }

        [Fact]
        public void BackgroundServiceOptions_ShouldAllowSettingEnableDetailedLogging()
        {
            // Arrange
            var options = new BackgroundServiceOptions();

            // Act
            options.EnableDetailedLogging = true;

            // Assert
            Assert.True(options.EnableDetailedLogging);
        }

        [Fact]
        public void ChannelFullMode_ShouldHaveThreeValues()
        {
            // Arrange & Act
            var values = Enum.GetValues<ChannelFullMode>();

            // Assert
            Assert.Equal(3, values.Length);
            Assert.Contains(ChannelFullMode.Wait, values);
            Assert.Contains(ChannelFullMode.DropOldest, values);
            Assert.Contains(ChannelFullMode.ThrowException, values);
        }

        [Theory]
        [InlineData(ChannelFullMode.Wait)]
        [InlineData(ChannelFullMode.DropOldest)]
        [InlineData(ChannelFullMode.ThrowException)]
        public void ChannelFullMode_ShouldBeValidEnumValue(ChannelFullMode mode)
        {
            // Arrange
            var options = new BackgroundServiceOptions();

            // Act
            options.ChannelFullMode = mode;

            // Assert
            Assert.Equal(mode, options.ChannelFullMode);
        }

        [Fact]
        public void BackgroundServiceOptions_ShouldSupportPropertyInitialization()
        {
            // Arrange & Act
            var options = new BackgroundServiceOptions
            {
                UseGlobalStoppingToken = true,
                MaxConcurrentWorkers = 8,
                ChannelCapacity = 100,
                ChannelFullMode = ChannelFullMode.ThrowException,
                EnableDetailedLogging = true
            };

            // Assert
            Assert.True(options.UseGlobalStoppingToken);
            Assert.Equal(8, options.MaxConcurrentWorkers);
            Assert.Equal(100, options.ChannelCapacity);
            Assert.Equal(ChannelFullMode.ThrowException, options.ChannelFullMode);
            Assert.True(options.EnableDetailedLogging);
        }
    }
}


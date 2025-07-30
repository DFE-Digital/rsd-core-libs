using DfE.CoreLibs.Http.Utils;
using FluentAssertions;
using System.Text.RegularExpressions;
using Xunit;

namespace DfE.CoreLibs.Http.Tests.Utils
{
    public class ErrorIdGeneratorTests
    {
        [Fact]
        public void GenerateDefault_ShouldReturnSixDigitNumber()
        {
            // Act
            var result = ErrorIdGenerator.GenerateDefault();

            // Assert
            result.Should().Match(@"^\d{6}$");
            int.Parse(result).Should().BeGreaterThanOrEqualTo(100000);
            int.Parse(result).Should().BeLessThanOrEqualTo(999999);
        }

        [Fact]
        public void GenerateDefault_ShouldReturnDifferentValues_OnMultipleCalls()
        {
            // Act
            var result1 = ErrorIdGenerator.GenerateDefault();
            var result2 = ErrorIdGenerator.GenerateDefault();

            // Assert
            result1.Should().NotBe(result2);
        }

        [Theory]
        [InlineData("Development", "D")]
        [InlineData("Dev", "D")]
        [InlineData("Test", "T")]
        [InlineData("Staging", "T")]
        [InlineData("Production", "P")]
        [InlineData("Prod", "P")]
        [InlineData("UAT", "U")]
        [InlineData("QA", "Q")]
        [InlineData("Unknown", "X")]
        [InlineData("", "X")]
        [InlineData(null, "X")]
        public void GetEnvironmentPrefix_ShouldReturnExpectedPrefix(string environmentName, string expectedPrefix)
        {
            // Act
            var result = ErrorIdGenerator.GetEnvironmentPrefix(environmentName);

            // Assert
            result.Should().Be(expectedPrefix);
        }

        [Theory]
        [InlineData("Development")]
        [InlineData("Test")]
        [InlineData("Production")]
        [InlineData("UAT")]
        [InlineData("QA")]
        public void GenerateDefault_WithEnvironment_ShouldReturnPrefixedSixDigitNumber(string environment)
        {
            // Act
            var result = ErrorIdGenerator.GenerateDefault(environment);

            // Assert
            var prefix = ErrorIdGenerator.GetEnvironmentPrefix(environment);
            result.Should().Match($"^{prefix}-\\d{{6}}$");
        }

        [Fact]
        public void GenerateTimestampBased_ShouldReturnExpectedFormat()
        {
            // Act
            var result = ErrorIdGenerator.GenerateTimestampBased();

            // Assert
            result.Should().Match(@"^\d{8}-\d{6}-\d{4}$");
            
            // Parse and validate components
            var parts = result.Split('-');
            parts.Should().HaveCount(3);
            
            // Validate date part (YYYYMMDD)
            var datePart = parts[0];
            datePart.Should().HaveLength(8);
            DateTime.TryParseExact(datePart, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out _).Should().BeTrue();
            
            // Validate time part (HHMMSS)
            var timePart = parts[1];
            timePart.Should().HaveLength(6);
            
            // Validate random part (XXXX)
            var randomPart = parts[2];
            randomPart.Should().HaveLength(4);
            int.Parse(randomPart).Should().BeGreaterThanOrEqualTo(1000);
            int.Parse(randomPart).Should().BeLessThanOrEqualTo(9999);
        }

        [Theory]
        [InlineData("Development")]
        [InlineData("Test")]
        [InlineData("Production")]
        public void GenerateTimestampBased_WithEnvironment_ShouldReturnPrefixedFormat(string environment)
        {
            // Act
            var result = ErrorIdGenerator.GenerateTimestampBased(environment);

            // Assert
            var prefix = ErrorIdGenerator.GetEnvironmentPrefix(environment);
            result.Should().Match($"^{prefix}-\\d{{8}}-\\d{{6}}-\\d{{4}}$");
        }

        [Fact]
        public void GenerateGuidBased_ShouldReturnEightCharacterString()
        {
            // Act
            var result = ErrorIdGenerator.GenerateGuidBased();

            // Assert
            result.Should().HaveLength(8);
            result.Should().Match(@"^[a-f0-9]{8}$");
        }

        [Fact]
        public void GenerateGuidBased_ShouldReturnDifferentValues_OnMultipleCalls()
        {
            // Act
            var result1 = ErrorIdGenerator.GenerateGuidBased();
            var result2 = ErrorIdGenerator.GenerateGuidBased();

            // Assert
            result1.Should().NotBe(result2);
        }

        [Theory]
        [InlineData("Development")]
        [InlineData("Test")]
        [InlineData("Production")]
        public void GenerateGuidBased_WithEnvironment_ShouldReturnPrefixedFormat(string environment)
        {
            // Act
            var result = ErrorIdGenerator.GenerateGuidBased(environment);

            // Assert
            var prefix = ErrorIdGenerator.GetEnvironmentPrefix(environment);
            result.Should().Match($"^{prefix}-[a-f0-9]{{8}}$");
        }

        [Fact]
        public void GenerateSequential_ShouldReturnUnixTimestamp()
        {
            // Act
            var result = ErrorIdGenerator.GenerateSequential();

            // Assert
            result.Should().Match(@"^\d{13}$"); // Unix timestamp in milliseconds
            
            var timestamp = long.Parse(result);
            var expectedMinTimestamp = DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeMilliseconds();
            var expectedMaxTimestamp = DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeMilliseconds();
            
            timestamp.Should().BeGreaterThanOrEqualTo(expectedMinTimestamp);
            timestamp.Should().BeLessThanOrEqualTo(expectedMaxTimestamp);
        }

        [Theory]
        [InlineData("Development")]
        [InlineData("Test")]
        [InlineData("Production")]
        public void GenerateSequential_WithEnvironment_ShouldReturnPrefixedFormat(string environment)
        {
            // Act
            var result = ErrorIdGenerator.GenerateSequential(environment);

            // Assert
            var prefix = ErrorIdGenerator.GetEnvironmentPrefix(environment);
            result.Should().Match($"^{prefix}-\\d{{13}}$");
        }

        [Theory]
        [InlineData("Development")]
        [InlineData("Test")]
        [InlineData("Production")]
        [InlineData("UAT")]
        [InlineData("QA")]
        public void GenerateDefaultWithEnvironment_ShouldReturnPrefixedSixDigitNumber(string environmentName)
        {
            // Act
            var result = ErrorIdGenerator.GenerateDefaultWithEnvironment(environmentName);

            // Assert
            var prefix = ErrorIdGenerator.GetEnvironmentPrefix(environmentName);
            result.Should().Match($"^{prefix}-\\d{{6}}$");
        }

        [Theory]
        [InlineData("Development")]
        [InlineData("Test")]
        [InlineData("Production")]
        [InlineData("UAT")]
        [InlineData("QA")]
        public void GenerateTimestampBasedWithEnvironment_ShouldReturnPrefixedFormat(string environmentName)
        {
            // Act
            var result = ErrorIdGenerator.GenerateTimestampBasedWithEnvironment(environmentName);

            // Assert
            var prefix = ErrorIdGenerator.GetEnvironmentPrefix(environmentName);
            result.Should().Match($"^{prefix}-\\d{{8}}-\\d{{6}}-\\d{{4}}$");
        }

        [Theory]
        [InlineData("Development")]
        [InlineData("Test")]
        [InlineData("Production")]
        [InlineData("UAT")]
        [InlineData("QA")]
        public void GenerateGuidBasedWithEnvironment_ShouldReturnPrefixedFormat(string environmentName)
        {
            // Act
            var result = ErrorIdGenerator.GenerateGuidBasedWithEnvironment(environmentName);

            // Assert
            var prefix = ErrorIdGenerator.GetEnvironmentPrefix(environmentName);
            result.Should().Match($"^{prefix}-[a-f0-9]{{8}}$");
        }

        [Theory]
        [InlineData("Development")]
        [InlineData("Test")]
        [InlineData("Production")]
        [InlineData("UAT")]
        [InlineData("QA")]
        public void GenerateSequentialWithEnvironment_ShouldReturnPrefixedFormat(string environmentName)
        {
            // Act
            var result = ErrorIdGenerator.GenerateSequentialWithEnvironment(environmentName);

            // Assert
            var prefix = ErrorIdGenerator.GetEnvironmentPrefix(environmentName);
            result.Should().Match($"^{prefix}-\\d{{13}}$");
        }

        [Fact]
        public void GenerateDefault_ShouldBeThreadSafe()
        {
            // Arrange
            var tasks = new List<Task<string>>();
            var results = new List<string>();

            // Act
            for (int i = 0; i < 100; i++)
            {
                tasks.Add(Task.Run(() => ErrorIdGenerator.GenerateDefault()));
            }

            Task.WaitAll(tasks.ToArray());
            results.AddRange(tasks.Select(t => t.Result));

            // Assert
            results.Should().HaveCount(100);
            results.Should().OnlyContain(r => Regex.IsMatch(r, @"^\d{6}$"));

            results.Distinct().Should().HaveCount(100); // All should be unique
        }

        [Fact]
        public void GenerateTimestampBased_ShouldBeThreadSafe()
        {
            // Arrange
            var tasks = new List<Task<string>>();
            var results = new List<string>();

            // Act
            for (int i = 0; i < 100; i++)
            {
                tasks.Add(Task.Run(() => ErrorIdGenerator.GenerateTimestampBased()));
            }

            Task.WaitAll(tasks.ToArray());
            results.AddRange(tasks.Select(t => t.Result));

            // Assert
            results.Should().HaveCount(100);
            results.Should().OnlyContain(r => Regex.IsMatch(r, @"^\d{8}-\d{6}-\d{4}$"));
        }

        [Fact]
        public void GenerateGuidBased_ShouldBeThreadSafe()
        {
            // Arrange
            var tasks = new List<Task<string>>();
            var results = new List<string>();

            // Act
            for (int i = 0; i < 100; i++)
            {
                tasks.Add(Task.Run(() => ErrorIdGenerator.GenerateGuidBased()));
            }

            Task.WaitAll(tasks.ToArray());
            results.AddRange(tasks.Select(t => t.Result));

            // Assert
            results.Should().HaveCount(100);
            results.Should().OnlyContain(r => Regex.IsMatch(r, @"^[a-f0-9]{8}$"));
            results.Distinct().Should().HaveCount(100); // All should be unique
        }

        [Fact]
        public void GenerateSequential_ShouldBeThreadSafe()
        {
            // Arrange
            var tasks = new List<Task<string>>();
            var results = new List<string>();

            // Act
            for (int i = 0; i < 100; i++)
            {
                tasks.Add(Task.Run(() => ErrorIdGenerator.GenerateSequential()));
            }

            Task.WaitAll(tasks.ToArray());
            results.AddRange(tasks.Select(t => t.Result));

            // Assert
            results.Should().HaveCount(100);
            results.Should().OnlyContain(r => Regex.IsMatch(r, @"^\d{13}$"));
        }

        [Theory]
        [InlineData("development", "D")]
        [InlineData("DEV", "D")]
        [InlineData("test", "T")]
        [InlineData("STAGING", "T")]
        [InlineData("production", "P")]
        [InlineData("PROD", "P")]
        [InlineData("uat", "U")]
        [InlineData("qa", "Q")]
        public void GetEnvironmentPrefix_ShouldBeCaseInsensitive(string environmentName, string expectedPrefix)
        {
            // Act
            var result = ErrorIdGenerator.GetEnvironmentPrefix(environmentName);

            // Assert
            result.Should().Be(expectedPrefix);
        }
    }
} 
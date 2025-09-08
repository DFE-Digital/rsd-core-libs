using GovUK.Dfe.CoreLibs.Utilities.Extensions;
using FluentAssertions;
using FluentAssertions.Execution;

namespace GovUK.Dfe.CoreLibs.Utilities.Tests.Extensions;

public class DecimalExtensionsTests
{
    [Fact]
    public void ToPercentage_Returns_PercentageString()
    {
        var input = new decimal(1.5);
        input.ToPercentage().Should().Be("1.5%");
    }


    [Theory]
    [InlineData("1100", "1100")]
    [InlineData("1100.1", "1100.1")]
    [InlineData("1100.100", "1100.1")]
    [InlineData("1100.1000", "1100.1")]
    [InlineData("1100.1001", "1100.1001")]
    [InlineData("1100.1001000", "1100.1001")]
    public void ToSafeString_Formats_Decimals_Correctly(string inputDecimal, string expectation)
    {
        decimal? i = Convert.ToDecimal(inputDecimal);
        i.ToSafeString().Should().Be(expectation);
    }

    [Fact]
    public void ToSafeString_With_Null_Decimal_Returns_EmptyString()
    {
        decimal? input = null;
        input.ToSafeString().Should().Be(string.Empty);
    }

    [Theory]
    [InlineData("1100", false, "1100.00")]
    [InlineData("1100", true, "£1,100.00")]
    [InlineData("1100.1", false, "1100.10")]
    [InlineData("1100.1", true, "£1,100.10")]
    [InlineData("1100.100", false, "1100.10")]
    [InlineData("1100.100", true, "£1,100.10")]
    [InlineData("1100.1000", false, "1100.10")]
    [InlineData("1100.1000", true, "£1,100.10")]
    [InlineData("1100.1001", false, "1100.10")]
    [InlineData("1100.1001", true, "£1,100.10")]
    [InlineData("1100.1001000", false, "1100.10")]
    [InlineData("1100.1001000", true, "£1,100.10")]

    public void ToMoneyString_With_NullableDecimal_Formats_String_Correctly(string inputDecimal, bool includePoundSign, string expectation)
    {
        decimal? i = Convert.ToDecimal(inputDecimal);
        using var scope = new AssertionScope();
        scope.AddReportable(nameof(inputDecimal), inputDecimal);
        scope.AddReportable(nameof(includePoundSign), includePoundSign.ToString);
        i.ToMoneyString(includePoundSign).Should().Be(expectation);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ToMoneyString_With_NullDecimal_Returns_EmptyString(bool includePoundSign)
    {
        decimal? i = null;
        i.ToMoneyString(includePoundSign).Should().Be(string.Empty);
    }

    [Theory]
    [InlineData("1100", false, "1100.00")]
    [InlineData("1100", true, "£1,100.00")]
    [InlineData("1100.1", false, "1100.10")]
    [InlineData("1100.1", true, "£1,100.10")]
    [InlineData("1100.100", false, "1100.10")]
    [InlineData("1100.100", true, "£1,100.10")]
    [InlineData("1100.1000", false, "1100.10")]
    [InlineData("1100.1000", true, "£1,100.10")]
    [InlineData("1100.1001", false, "1100.10")]
    [InlineData("1100.1001", true, "£1,100.10")]
    [InlineData("1100.1001000", false, "1100.10")]
    [InlineData("1100.1001000", true, "£1,100.10")]

    public void ToMoneyString_With_Decimal_Formats_String_Correctly(string inputDecimal, bool includePoundSign, string expectation)
    {
        decimal i = Convert.ToDecimal(inputDecimal);
        using var scope = new AssertionScope();
        scope.AddReportable(nameof(inputDecimal), inputDecimal);
        scope.AddReportable(nameof(includePoundSign), includePoundSign.ToString);
        i.ToMoneyString(includePoundSign).Should().Be(expectation);
    }
}

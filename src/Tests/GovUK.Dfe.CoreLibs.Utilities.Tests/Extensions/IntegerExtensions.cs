using GovUK.Dfe.CoreLibs.Utilities.Extensions;
using FluentAssertions;

namespace GovUK.Dfe.CoreLibs.Utilities.Tests.Extensions;

/// <summary>
/// The integer extensions tests.
/// </summary>

public class IntegerExtensionsTests
{
    [Fact]
    public void AsPercentageOf_With_Null_Part_Returns_EmptyString()
    {
        int? part = null;
        int? whole = 100;
        part.AsPercentageOf(whole).Should().Be(string.Empty);
    }

    [Fact]
    public void AsPercentageOf_With_Null_Whole_Returns_EmptyString()
    {
        int? part = 50;
        int? whole = null;
        part.AsPercentageOf(whole).Should().Be(string.Empty);
    }

    [Theory]
    [InlineData(50, 100, "50%")]
    [InlineData(100, 50, "200%")]
    [InlineData(-50, 100, "-50%")]
    [InlineData(-50, -100, "50%")]
    public void AsPercentageOf_With_Part_And_Whole_Returns_Percentage(int? part, int whole, string expectation)
    {
        part.AsPercentageOf(whole).Should().Be(expectation);
    }
    
    [Fact]
    public void AsPercentageOf_With_Part_And_Zero_Whole_Throws_Exception()
    {
        int? part = -50;
        int? whole = 0;

        Action act = () => part.AsPercentageOf(whole);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}

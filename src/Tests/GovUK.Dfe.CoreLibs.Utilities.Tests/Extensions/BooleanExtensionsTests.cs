using GovUK.Dfe.CoreLibs.Utilities.Extensions;
using FluentAssertions;

namespace GovUK.Dfe.CoreLibs.Utilities.Tests.Extensions;

/// <summary>
/// The boolean extensions tests.
/// </summary>
public class BooleanExtensionsTests
{
    [Fact]
    public void ToYesNoString_WithFalse_ReturnsNo()
    {
        false.ToYesNoString().Should().Be("No");
    }

    [Fact]
    public void ToYesNoString_WithTrue_ReturnsYes()
    {
        true.ToYesNoString().Should().Be("Yes");
    }
    
    [Fact]
    public void ToYesNoString_WithNullableFalse_ReturnsNo()
    {
        bool? input = false;
        input.ToYesNoString().Should().Be("No");
    }

    [Fact]
    public void ToYesNoString_WithNullableTrue_ReturnsYes()
    {
        bool? input = true;
        input.ToYesNoString().Should().Be("Yes");
    }
    
    [Fact]
    public void ToYesNoString_WithNullableBool_ReturnsEmptyString()
    {
        bool? input = null;
        input.ToYesNoString().Should().Be(string.Empty);
    }

    [Fact]
    public void ToSurplusDeficitString_WithFalse_ReturnsSurplus()
    {
        false.ToSurplusDeficitString().Should().Be("Surplus");
    }

    [Fact]
    public void ToSurplusDeficitString_WithTrue_ReturnsDeficit()
    {
        true.ToSurplusDeficitString().Should().Be("Deficit");
    }

    [Fact]
    public void ToSurplusDeficitString_WithNullableFalse_ReturnsSurplus()
    {
        bool? input = false;
        input.ToSurplusDeficitString().Should().Be("Surplus");
    }

    [Fact]
    public void ToSurplusDeficitString_WithNullableTrue_ReturnsDeficit()
    {
        bool? input = true;
        input.ToSurplusDeficitString().Should().Be("Deficit");
    }
    
    [Fact]
    public void ToSurplusDeficitString_WithNullableBool_ReturnsEmptyString()
    {
        bool? input = null;
        input.ToSurplusDeficitString().Should().Be(string.Empty);
    }
}

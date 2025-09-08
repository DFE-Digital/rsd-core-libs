using GovUK.Dfe.CoreLibs.Utilities.Extensions;
using FluentAssertions;

namespace GovUK.Dfe.CoreLibs.Utilities.Tests.Extensions;

/// <summary>
/// The enum extensions tests.
/// </summary>
public class EnumExtensionsTests
{
    [Theory]
    [InlineData(ExampleEnum.DescriptionWithSpaces, "Regional Director for the region")]
    [InlineData(ExampleEnum.DescriptionWithOneWord, "DescriptionWithOneWord")]
    [InlineData(ExampleEnum.WhiteSpaceDescription, " ")]
    [InlineData(ExampleEnum.NullDescription, default)]
    [InlineData(ExampleEnum.EmptyDescription, "")]
    public void Should_return_the_description_of_enum(ExampleEnum input, string expectedDescription) =>
        input.ToDescription().Should().Be(expectedDescription);

    [Theory]
    [InlineData(ExampleEnum.NoDescription, "NoDescription")]
    [InlineData(null, "")]
    public void Should_return_the_name_of_enum_when_no_description(ExampleEnum? input, string expectedDescription) => 
        input.ToDescription().Should().Be(expectedDescription);
    //}
}

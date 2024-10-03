using DfE.CoreLibs.Utilities.Extensions;
using FluentAssertions;

namespace DfE.CoreLibs.Utilities.Tests.Extensions;

/// <summary>
/// The object extension tests.
/// </summary>
public class ObjectExtensionTests
{
    [Fact]
    public void ToStringOrDefault_WithNoDefault_Returns_StringDefault()
    {
        string? str = null;
        str.ToStringOrDefault().Should().Be(default(string));

        int? i = null;
        i.ToStringOrDefault().Should().Be(default(string));
    }

    [Fact]
    public void ToStringOrDefault_WithDefault_Returns_StringArgument()
    {
        string? str = null;
        str.ToStringOrDefault("MyDefault").Should().Be("MyDefault");

        int? i = null;
        i.ToStringOrDefault("MyDefault").Should().Be("MyDefault");
    }
}

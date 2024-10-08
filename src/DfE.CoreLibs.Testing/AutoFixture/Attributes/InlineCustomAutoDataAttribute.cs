using System.Diagnostics.CodeAnalysis;
using AutoFixture.Xunit2;

namespace DfE.CoreLibs.Testing.AutoFixture.Attributes
{
    [ExcludeFromCodeCoverage]
    [AttributeUsage(AttributeTargets.Method)]
    public class InlineCustomAutoDataAttribute(object[] values, params Type[] customizations)
        : InlineAutoDataAttribute(new CustomAutoDataAttribute(customizations), values);
}

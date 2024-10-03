using AutoFixture;
using AutoFixture.AutoNSubstitute;
using System.Diagnostics.CodeAnalysis;

namespace DfE.CoreLibs.Testing.AutoFixture.Customizations
{
    [ExcludeFromCodeCoverage]
    public class NSubstituteCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Customize(new AutoNSubstituteCustomization());
        }
    }
}

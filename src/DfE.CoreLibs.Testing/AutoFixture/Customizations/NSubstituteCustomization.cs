using AutoFixture;
using AutoFixture.AutoNSubstitute;

namespace DfE.CoreLibs.Testing.AutoFixture.Customizations
{
    public class NSubstituteCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Customize(new AutoNSubstituteCustomization());
        }
    }
}

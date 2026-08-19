using AutoFixture;
using AutoFixture.AutoNSubstitute;
using System.Diagnostics.CodeAnalysis;

namespace GovUK.Dfe.CoreLibs.Testing.AutoFixture.Customizations
{
    [ExcludeFromCodeCoverage]
    public class NSubstituteCustomization : ICustomization
    {
        private readonly bool _configureMembers;

        public NSubstituteCustomization() : this(false) { }

        public NSubstituteCustomization(bool configureMembers)
        {
            _configureMembers = configureMembers;
        }

        public void Customize(IFixture fixture)
        {
            fixture.Customize(new AutoNSubstituteCustomization { ConfigureMembers = _configureMembers });
        }
    }
}

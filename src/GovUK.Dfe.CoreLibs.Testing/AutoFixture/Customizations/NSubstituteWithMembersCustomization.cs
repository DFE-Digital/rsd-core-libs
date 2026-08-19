using AutoFixture;
using AutoFixture.AutoNSubstitute;
using System.Diagnostics.CodeAnalysis;

namespace GovUK.Dfe.CoreLibs.Testing.AutoFixture.Customizations
{
    /// <summary>
    /// NSubstitute customization with <c>ConfigureMembers = true</c>, enabling
    /// auto-configured return values on substitute members.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class NSubstituteWithMembersCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Customize(new AutoNSubstituteCustomization { ConfigureMembers = true });
        }
    }
}

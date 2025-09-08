using System.Diagnostics.CodeAnalysis;
using AutoFixture.Xunit2;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Customizations;
using GovUK.Dfe.CoreLibs.Testing.Helpers;

namespace GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes
{
    [ExcludeFromCodeCoverage]
    [AttributeUsage(AttributeTargets.Method)]
    public class CustomAutoDataAttribute(params Type[] customizations)
        : AutoDataAttribute(() => FixtureFactoryHelper.ConfigureFixtureFactory(CombineCustomizations(customizations)))
    {
        private static Type[] CombineCustomizations(Type[] customizations)
        {
            var defaultCustomizations = new[] { typeof(NSubstituteCustomization) };
            return defaultCustomizations.Concat(customizations).ToArray();
        }
    }
}

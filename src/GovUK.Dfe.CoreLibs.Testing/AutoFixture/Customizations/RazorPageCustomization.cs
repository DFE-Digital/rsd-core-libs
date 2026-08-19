using AutoFixture;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics.CodeAnalysis;

namespace GovUK.Dfe.CoreLibs.Testing.AutoFixture.Customizations
{
    /// <summary>
    /// Handles circular-reference and unresolvable types commonly encountered
    /// when AutoFixture tries to create ASP.NET Core Razor Pages infrastructure types.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class RazorPageCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Customize<CompiledPageActionDescriptor>(ob => ob
                .Without(d => d.HandlerMethods)
                .Without(d => d.Parameters)
                .Without(d => d.BoundProperties));

            fixture.Customize<ActionDescriptor>(ob => ob
                .Without(d => d.Parameters)
                .Without(d => d.BoundProperties));

            fixture.Register(() => new DefaultHttpContext());
        }
    }
}

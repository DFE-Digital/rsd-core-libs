using AutoFixture;
using Microsoft.AspNetCore.Http;

namespace DfE.CoreLibs.Testing.AutoFixture.Customizations
{
    public class HttpContextCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Register(() => new DefaultHttpContext());
        }
    }
}
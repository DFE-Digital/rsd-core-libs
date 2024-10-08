﻿using AutoFixture;
using Microsoft.AspNetCore.Http;
using System.Diagnostics.CodeAnalysis;

namespace DfE.CoreLibs.Testing.AutoFixture.Customizations
{
    [ExcludeFromCodeCoverage]
    public class HttpContextCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Register(() => new DefaultHttpContext());
        }
    }
}
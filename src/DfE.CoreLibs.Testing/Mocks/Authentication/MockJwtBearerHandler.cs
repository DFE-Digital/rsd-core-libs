﻿using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace DfE.CoreLibs.Testing.Mocks.Authentication
{
#pragma warning disable CS0618
    [ExcludeFromCodeCoverage]
    public class MockJwtBearerHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        IEnumerable<Claim> claims)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder, clock)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var identity = new ClaimsIdentity(claims, "mock");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "mock");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
#pragma warning restore CS0618

}
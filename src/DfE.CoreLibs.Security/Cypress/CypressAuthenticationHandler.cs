using DfE.CoreLibs.Security.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace DfE.CoreLibs.Security.Cypress
{
    /// <summary>
    /// An authentication handler that builds a claims principal from
    /// custom headers, intended for testing or "Cypress" scenarios.
    /// </summary>
    public class CypressAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IHttpContextAccessor httpContextAccessor)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        private const string ObjectIdClaimType = "http://schemas.microsoft.com/identity/claims/objectidentifier";
        private const string TestFallbackObjectId = "TEST-AD-ID";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return Task.FromResult(AuthenticateResult.Fail("No HttpContext"));
            }

            var userId = httpContext.Request.Headers["x-user-context-id"].FirstOrDefault() ?? Guid.NewGuid().ToString();

            var headers = httpContext.Request.Headers
                .Select(x => new KeyValuePair<string, string?>(x.Key, x.Value[0]))
                .ToArray();

            var userInfo = ParsedUserContext.FromHeaders(headers!);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, userInfo!.Name),
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Authentication, "true")
            };

            if (!string.IsNullOrEmpty(userInfo.AdId))
            {
                claims.Add(new Claim(ObjectIdClaimType, userInfo.AdId));
            }
            else if (!claims.Exists(c => c.Type == ObjectIdClaimType))
            {
                claims.Add(new Claim(ObjectIdClaimType, TestFallbackObjectId));
            }

            foreach (var claim in userInfo.Roles ?? [])
            {
                claims.Add(new Claim(ClaimTypes.Role, claim));
            }

            var claimsIdentity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(claimsIdentity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

    }
}

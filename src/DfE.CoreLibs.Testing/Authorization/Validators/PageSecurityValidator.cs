using DfE.CoreLibs.Testing.Authorization.Helpers;
using DfE.CoreLibs.Testing.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;

namespace DfE.CoreLibs.Testing.Authorization.Validators
{
    public class PageSecurityValidator(RouteEndpoint endpoint, bool globalAuthorizationEnabled = false)
    {
        public ValidationResult ValidateSinglePageSecurity(string route, string expectedSecurity)
        {
            var hasAuthorizeMetadata = endpoint.Metadata.Any(m => m is AuthorizeAttribute);
            var authorizeAttributes = endpoint.Metadata.OfType<AuthorizeAttribute>().ToList();

            if (globalAuthorizationEnabled)
            { 
                if (expectedSecurity == "AllowAnonymous")
                {
                    if (hasAuthorizeMetadata)
                    {
                        return ValidationResult.Failed($"Page {route} should be anonymous but is protected.");
                    }
                }
                else
                {
                    if (!hasAuthorizeMetadata)
                    {
                        return ValidationResult.Failed($"Page {route} should be protected globally but has no Authorize attribute.");
                    }
                }
            }

            if (expectedSecurity.StartsWith("Authorize"))
            {
                var expectedRequirements = ValidatorHelper.ParseExpectedSecurity(expectedSecurity);
                try
                {
                    ValidatorHelper.ValidateAuthorizeAttributes(authorizeAttributes, route, expectedRequirements);
                }
                catch (Exception ex)
                {
                    return ValidationResult.Failed(ex.Message);
                }
            }

            return ValidationResult.Success();
        }
    }
}

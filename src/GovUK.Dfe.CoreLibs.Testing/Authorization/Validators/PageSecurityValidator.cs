using GovUK.Dfe.CoreLibs.Testing.Authorization.Helpers;
using GovUK.Dfe.CoreLibs.Testing.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;

namespace GovUK.Dfe.CoreLibs.Testing.Authorization.Validators
{ 
    public class PageSecurityValidator(RouteEndpoint endpoint, bool globalAuthorizationEnabled = false)
    {
        public ValidationResult ValidateSinglePageSecurity(string route, string expectedSecurity)
        {
            var hasAuthorizeMetadata = endpoint.Metadata.Any(m => m is AuthorizeAttribute);
            var hasAllowAnonymousMetadata = endpoint.Metadata.Any(m => m is AllowAnonymousAttribute);
            var authorizeAttributes = endpoint.Metadata.OfType<AuthorizeAttribute>().ToList();

            if (globalAuthorizationEnabled && expectedSecurity != "AllowAnonymous" && !hasAuthorizeMetadata)
            {
                return ValidationResult.Failed($"Page {route} should be protected globally but has no Authorize attribute.");
            }

            return expectedSecurity switch
            {
                "AllowAnonymous" => ValidateAllowAnonymousPage(route, hasAuthorizeMetadata, hasAllowAnonymousMetadata),
                var security when security.StartsWith("Authorize") => ValidateAuthorizePage(route, authorizeAttributes, expectedSecurity, hasAllowAnonymousMetadata),
                _ => ValidationResult.Success()
            };
        }

        private static ValidationResult ValidateAllowAnonymousPage(string route, bool hasAuthorizeMetadata, bool hasAllowAnonymousMetadata)
        {
            // AllowAnonymous page checks
            if (hasAllowAnonymousMetadata)
                return ValidationResult.Success();

            return hasAuthorizeMetadata
                ? ValidationResult.Failed($"Page {route} should be anonymous but is protected.")
                : ValidationResult.Success();
        }

        private static ValidationResult ValidateAuthorizePage(string route, List<AuthorizeAttribute> authorizeAttributes, string expectedSecurity, bool hasAllowAnonymousMetadata)
        {
            if (hasAllowAnonymousMetadata)
                return ValidationResult.Failed($"Page {route} should be protected but is anonymous.");

            var expectedRequirements = ValidatorHelper.ParseExpectedSecurity(expectedSecurity);

            try
            {
                ValidatorHelper.ValidateAuthorizeAttributes(authorizeAttributes, route, expectedRequirements);
                return ValidationResult.Success();
            }
            catch (Exception ex)
            {
                return ValidationResult.Failed(ex.Message);
            }
        }
    }
}

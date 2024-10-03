using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using DfE.CoreLibs.Testing.Authorization.Helpers;
using DfE.CoreLibs.Testing.Results;
using Microsoft.AspNetCore.Authorization;

namespace DfE.CoreLibs.Testing.Authorization.Validators
{
    [ExcludeFromCodeCoverage]
    public class AuthorizationValidator(
        Dictionary<string, string> security,
        bool globalAuthorizationEnabled = true)
    {
        /// <summary>
        /// Validates controller and method authorization.
        /// </summary>
        /// <param name="controller">The controller type.</param>
        /// <param name="method">The method info.</param>
        public ValidationResult ValidateSecurity(Type controller, MethodInfo method)
        {
            var key = $"{controller.Name}.{method.Name}";

            if (!security.TryGetValue(key, out var expectedSecurity))
            {
                return ValidationResult.Failed($"No security configuration found for {key}");
            }

            var methodAuthorizeAttributes = method.GetCustomAttributes<AuthorizeAttribute>().ToList();
            var methodAllowAnonymousAttributes = method.GetCustomAttributes<AllowAnonymousAttribute>().ToList();

            var classAuthorizeAttributes = controller.GetCustomAttributes<AuthorizeAttribute>().ToList();
            var classAllowAnonymousAttributes = controller.GetCustomAttributes<AllowAnonymousAttribute>().ToList();

            var effectiveAuthorizeAttributes = methodAuthorizeAttributes.Any() ? methodAuthorizeAttributes : classAuthorizeAttributes;
            var effectiveAllowAnonymousAttributes = methodAllowAnonymousAttributes.Any() ? methodAllowAnonymousAttributes : classAllowAnonymousAttributes;

            if (!effectiveAuthorizeAttributes.Any() && !effectiveAllowAnonymousAttributes.Any())
            {
                if (globalAuthorizationEnabled && expectedSecurity != "AllowAnonymous")
                {
                    return ValidationResult.Success();
                }
                else if (expectedSecurity != "AllowAnonymous")
                {
                    return ValidationResult.Failed($"Expected {key} to be protected, but no Authorize attribute was found.");
                }
            }

            if (expectedSecurity == "AllowAnonymous")
            {
                if (!effectiveAllowAnonymousAttributes.Any())
                {
                    return ValidationResult.Failed($"Expected {key} to allow anonymous access but it is protected.");
                }
                return ValidationResult.Success();
            }

            if (expectedSecurity.StartsWith("Authorize"))
            {
                if (!effectiveAuthorizeAttributes.Any())
                {
                    return ValidationResult.Failed($"Expected {key} to have Authorize attribute.");
                }

                if (expectedSecurity == "Authorize")
                {
                    return ValidationResult.Success();
                }

                var requirements = ValidatorHelper.ParseExpectedSecurity(expectedSecurity);
                try
                {
                    ValidatorHelper.ValidateAuthorizeAttributes(effectiveAuthorizeAttributes, key, requirements);
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

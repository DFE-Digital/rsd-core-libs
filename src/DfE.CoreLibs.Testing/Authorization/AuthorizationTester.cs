using DfE.CoreLibs.Testing.Authorization.Validators;
using DfE.CoreLibs.Testing.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace DfE.CoreLibs.Testing.Authorization
{
    [ExcludeFromCodeCoverage]
    public class AuthorizationTester(bool globalAuthorizationEnabled = false)
    {
        public ValidationResult ValidateEndpoint(Assembly assembly, string controllerName, string actionName, string expectedSecurity)
        {
            var key = $"{controllerName}.{actionName}";
            var security = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(expectedSecurity))
            {
                return ValidationResult.Failed($"No security configuration found for endpoint '{key}'.");
            }
            else
            {
                security[key] = expectedSecurity;
            }

            var controllerType = Array.Find(assembly.GetTypes(), t => t.Name == controllerName && typeof(ControllerBase).IsAssignableFrom(t));

            if (controllerType == null)
            {
                return ValidationResult.Failed($"Controller '{controllerName}' not found.");
            }

            var method = Array.Find(controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly),
               m => m.Name == actionName && m.IsPublic && !m.IsDefined(typeof(NonActionAttribute)));

            if (method == null)
            {
                return ValidationResult.Failed($"Action '{actionName}' not found in controller '{controllerName}'.");
            }

            var validator = new AuthorizationValidator(security, globalAuthorizationEnabled);
            return validator.ValidateSecurity(controllerType, method);
        }

        /// <summary>
        /// Validates the security configuration for a specific page route.
        /// </summary>
        public ValidationResult ValidatePageSecurity(string route, string expectedSecurity, IEnumerable<RouteEndpoint> endpoints)
        {
            var endpoint = endpoints
                .FirstOrDefault(e => e.DisplayName!.Trim('/').Equals(route.Trim('/'), StringComparison.InvariantCultureIgnoreCase));

            if (endpoint == null) ValidationResult.Failed($"Route '{route}' not found.");

            var validator = new PageSecurityValidator(endpoint, globalAuthorizationEnabled);
            return validator.ValidateSinglePageSecurity(route, expectedSecurity);
        }
    }
}

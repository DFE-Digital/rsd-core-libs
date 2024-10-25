using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using DfE.CoreLibs.Testing.Authorization.Exceptions;
using DfE.CoreLibs.Testing.Authorization.Validators;
using Microsoft.AspNetCore.Mvc;

namespace DfE.CoreLibs.Testing.Authorization
{
    [ExcludeFromCodeCoverage]
    public class AuthorizationTester(bool globalAuthorizationEnabled = false)
    {
        public void ValidateEndpoint(Assembly assembly, string controllerName, string actionName, string expectedSecurity)
        {
            var key = $"{controllerName}.{actionName}";
            var security = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(expectedSecurity))
            {
                throw new MissingSecurityConfigurationException($"No security configuration found for endpoint '{key}'. Please define it in the configuration file.");
            }
            else
            {
                security[key] = expectedSecurity;
            }

            var controllerType = Array.Find(assembly.GetTypes(), t => t.Name == controllerName && typeof(ControllerBase).IsAssignableFrom(t));

            if (controllerType == null)
            {
                throw new Exception($"Controller '{controllerName}' not found.");
            }

            var method = Array.Find(controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly),
               m => m.Name == actionName && m.IsPublic && !m.IsDefined(typeof(NonActionAttribute)));

            if (method == null)
            {
                throw new Exception($"Action '{actionName}' not found in controller '{controllerName}'.");
            }

            var validator = new AuthorizationValidator(security, globalAuthorizationEnabled);
            validator.ValidateSecurity(controllerType, method);
        }
    }
}

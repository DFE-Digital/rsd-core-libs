using System.Diagnostics.CodeAnalysis;
using System.Reflection;
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
        public void ValidateSecurity(Type controller, MethodInfo method)
        {
            var key = $"{controller.Name}.{method.Name}";

            if (!security.TryGetValue(key, out var expectedSecurity))
            {
                throw new Exception($"No security configuration found for {key}");
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
                    return;
                }
                else if (expectedSecurity != "AllowAnonymous")
                {
                    throw new Exception($"Expected {key} to be protected but no Authorize attribute was found.");
                }
            }

            if (expectedSecurity == "AllowAnonymous")
            {
                if (!effectiveAllowAnonymousAttributes.Any())
                {
                    throw new Exception($"Expected {key} to allow anonymous access but it is protected.");
                }
                return;
            }

            if (expectedSecurity.StartsWith("Authorize"))
            {
                if (!effectiveAuthorizeAttributes.Any())
                {
                    throw new Exception($"Expected {key} to have Authorize attribute.");
                }

                if (expectedSecurity == "Authorize")
                {
                    return;
                }

                var requirements = ParseExpectedSecurity(expectedSecurity);

                ValidateAuthorizeAttributes(effectiveAuthorizeAttributes, key, requirements);
            }
        }

        private List<ExpectedRequirement> ParseExpectedSecurity(string expectedSecurity)
        {
            var requirements = new List<ExpectedRequirement>();

            if (expectedSecurity.Length > "Authorize:".Length)
            {
                var parts = expectedSecurity.Substring("Authorize:".Length).Split(';', StringSplitOptions.RemoveEmptyEntries);

                foreach (var part in parts)
                {
                    var keyValue = part.Split('=', StringSplitOptions.RemoveEmptyEntries);
                    if (keyValue.Length == 2)
                    {
                        requirements.Add(new ExpectedRequirement
                        {
                            Type = keyValue[0].Trim(),
                            Values = keyValue[1].Split(',', StringSplitOptions.RemoveEmptyEntries).Select(v => v.Trim()).ToList()
                        });
                    }
                }
            }

            return requirements;
        }

        private static void ValidateAuthorizeAttributes(List<AuthorizeAttribute> authorizeAttributes, string key, List<ExpectedRequirement> requirements)
        {
            foreach (var requirement in requirements)
            {
                switch (requirement.Type)
                {
                    case "Policy":
                        foreach (var expectedPolicy in requirement.Values!)
                        {
                            if (!authorizeAttributes.Exists(attr => attr.Policy?.Split(',').Contains(expectedPolicy) == true))
                            {
                                throw new Exception($"Expected {key} to have Policy '{expectedPolicy}' but it was not found.");
                            }
                        }
                        break;
                    case "Roles":
                        foreach (var expectedRole in requirement.Values!)
                        {
                            if (!authorizeAttributes.Exists(attr => attr.Roles?.Split(',').Contains(expectedRole) == true))
                            {
                                throw new Exception($"Expected {key} to have Role '{expectedRole}' but it was not found.");
                            }
                        }
                        break;

                    default:
                        throw new Exception($"Unknown authorization requirement type '{requirement.Type}' in expected security.");
                }
            }
        }

        private class ExpectedRequirement
        {
            public string? Type { get; init; }
            public List<string>? Values { get; init; }
        }
    }

}

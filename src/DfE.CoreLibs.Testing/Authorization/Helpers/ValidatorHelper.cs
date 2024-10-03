using Microsoft.AspNetCore.Authorization;

namespace DfE.CoreLibs.Testing.Authorization.Helpers
{
    public static class ValidatorHelper
    {
        public static List<ExpectedRequirement> ParseExpectedSecurity(string expectedSecurity)
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

        public static void ValidateAuthorizeAttributes(List<AuthorizeAttribute> authorizeAttributes, string key, List<ExpectedRequirement> requirements)
        {
            if (!requirements.Any())
            {
                if (authorizeAttributes.Exists(attr => !string.IsNullOrEmpty(attr.Policy) || !string.IsNullOrEmpty(attr.Roles)))
                {
                    throw new Exception($"Expected {key} to have a basic Authorize attribute with no Policy or Roles, but found either Policy or Roles defined.");
                }
                return;
            }

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
    }

    public class ExpectedRequirement
    {
        public string? Type { get; init; }
        public List<string>? Values { get; init; }
    }
}

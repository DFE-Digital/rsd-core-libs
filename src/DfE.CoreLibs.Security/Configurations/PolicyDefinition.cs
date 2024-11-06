using DfE.DomainDrivenDesignTemplate.Infrastructure.Security.Configurations;

namespace DfE.CoreLibs.Security.Configurations
{
    /// <summary>
    /// Represents a policy definition, including roles, claims, and custom requirements.
    /// </summary>
    public class PolicyDefinition
    {
        public required string Name { get; set; }
        public required string Operator { get; set; } = "OR"; // "AND" or "OR"
        public required List<string> Roles { get; set; }
        public List<ClaimDefinition>? Claims { get; set; }
    }
}

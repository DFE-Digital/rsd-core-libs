namespace GovUK.Dfe.CoreLibs.Security.Configurations
{
    /// <summary>
    /// Represents a policy definition, including roles, claims, and custom requirements.
    /// </summary>
    public class PolicyDefinition
    {
        public required string Name { get; set; }
        public required string Operator { get; set; } = "OR"; // "AND" or "OR"
        public required List<string> Roles { get; set; }
        public List<string>? Scopes { get; set; }
        public List<ClaimDefinition>? Claims { get; set; }
    }
}

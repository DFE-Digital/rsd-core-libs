namespace DfE.DomainDrivenDesignTemplate.Infrastructure.Security.Configurations
{
    public class ClaimDefinition
    {
        public required string Type { get; set; }
        public required List<string> Values { get; set; }
    }
}

namespace DfE.CoreLibs.Contracts.ExternalApplications.Models.Response
{
    public sealed record ApplicationResponseDetailsDto
    {
        public Guid ResponseId { get; init; }
        public string ResponseBody { get; init; } = null!;
        public DateTime CreatedOn { get; init; }
        public Guid CreatedBy { get; init; }
        public DateTime? LastModifiedOn { get; init; }
        public Guid? LastModifiedBy { get; init; }
    }
}

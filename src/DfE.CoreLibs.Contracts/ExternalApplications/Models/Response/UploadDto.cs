namespace DfE.CoreLibs.Contracts.ExternalApplications.Models.Response
{

    public class UploadDto
    {
        public Guid Id { get; set; }
        public Guid ApplicationId { get; set; }
        public Guid UploadedBy { get; set; }
        public UserDto? UploadedByUser { get; set; } = null;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string OriginalFileName { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public DateTime UploadedOn { get; set; }
    }
}

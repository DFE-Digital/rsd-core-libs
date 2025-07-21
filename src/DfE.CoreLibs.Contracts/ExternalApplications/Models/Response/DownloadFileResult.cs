namespace DfE.CoreLibs.Contracts.ExternalApplications.Models.Response
{
    public class DownloadFileResult
    {
        public Stream FileStream { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public string ContentType { get; set; } = null!;
    }
}

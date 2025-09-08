using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request
{
    public class UploadFileRequest
    {
        [FromForm(Name = "name")] 
        public string Name { get; set; } = default!;

        [FromForm(Name = "description")] 
        public string? Description { get; set; }

        [FromForm(Name = "file")] 
        public IFormFile File { get; set; } = default!;
    }
}

using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using System;

namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response
{
    public sealed class CustomApplicationStatusDto    {

        public Guid? CustomApplicationStatusId { get; set; }

        public Guid TemplateId { get; set; }

        public ApplicationStatus ApplicationStatus { get; set; }

        public string? Label { get; set; }

        public DateTime CreatedOn { get; set; }

        public Guid CreatedBy { get; set; }

    }

}
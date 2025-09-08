using System.ComponentModel;

namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums
{
    public enum ApplicationStatus
    {
        [Description("In progress")]
        InProgress = 0,
        [Description("Submitted")]
        Submitted = 1
    }
}

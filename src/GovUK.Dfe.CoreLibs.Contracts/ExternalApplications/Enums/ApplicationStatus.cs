using System.ComponentModel;

namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums
{
    public enum ApplicationStatus
    {
        [Description("Created")]
        Created = -1,
        [Description("In progress")]
        InProgress = 0,
        [Description("Submitted")]
        Submitted = 1,
        [Description("Deleted")]
        Deleted = 2
    }
}

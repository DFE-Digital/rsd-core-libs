namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request
{
    /// <summary>
    /// Invite a contributor onto a specific application.
    /// </summary>
    public class AddContributorRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// When set, these flags control direct Application / ApplicationFiles grants for this invite.
        /// Application Read is always granted. When null, the API uses the template contributor-role
        /// behaviour (User role → full edit; other roles → read-only).
        /// </summary>
        public ContributorInvitePermissions? Permissions { get; set; }
    }

    /// <summary>
    /// Lead-applicant choices for what the invitee may do on this application.
    /// </summary>
    public class ContributorInvitePermissions
    {
        /// <summary>Can edit or change this application.</summary>
        public bool ApplicationWrite { get; set; }

        /// <summary>Can view uploaded files.</summary>
        public bool ApplicationFilesRead { get; set; }

        /// <summary>Can upload new files.</summary>
        public bool ApplicationFilesWrite { get; set; }

        /// <summary>Can delete uploaded files.</summary>
        public bool ApplicationFilesDelete { get; set; }
    }
}

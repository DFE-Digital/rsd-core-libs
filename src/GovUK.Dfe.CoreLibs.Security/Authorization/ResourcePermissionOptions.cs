namespace GovUK.Dfe.CoreLibs.Security.Authorization
{
    /// <summary>
    /// Options controlling the built-in resource+action policies.
    /// </summary>
    public class ResourcePermissionOptions
    {
        /// <summary>
        /// Claim type under which the Custom Claims will have
        /// added "resource:action" claims (default "permission").
        /// </summary>
        public string ClaimType { get; set; } = "permission";

        /// <summary>
        /// The list of action names to auto-generate policies for.
        /// E.g. ["Read","Write","Delete"] will create "CanRead","CanWrite","CanDelete" policies.
        /// </summary>
        public List<string> Actions { get; set; } = [];

        /// <summary>
        /// Format string to turn an action name into a policy name.
        /// Default "Can{0}" : "CanRead","CanWrite",…
        /// </summary>
        public string PolicyNameFormat { get; set; } = "Can{0}";
    }
}

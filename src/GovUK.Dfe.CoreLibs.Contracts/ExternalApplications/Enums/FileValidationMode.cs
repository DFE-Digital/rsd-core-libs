namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums
{
    /// <summary>
    /// Per-template policy for whether failed or pending file validation blocks submit.
    /// </summary>
    public enum FileValidationMode
    {
        /// <summary>Files are not validated; submit ignores validation status.</summary>
        Off = 0,

        /// <summary>Block submit only when any file is <see cref="FileValidationStatus.Failed"/>.</summary>
        FailOnInvalid = 1,

        /// <summary>Every file that requires validation must be <see cref="FileValidationStatus.Passed"/>.</summary>
        RequirePassed = 2
    }
}

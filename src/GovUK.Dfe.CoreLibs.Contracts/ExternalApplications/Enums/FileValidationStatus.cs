namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums
{
    /// <summary>
    /// External (tenant) validation outcome stored on an uploaded file.
    /// Distinct from virus scan, which deletes infected files rather than marking them.
    /// </summary>
    public enum FileValidationStatus
    {
        NotRequired = 0,
        Pending = 1,
        Passed = 2,
        Failed = 3
    }
}

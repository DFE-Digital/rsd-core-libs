namespace GovUK.Dfe.CoreLibs.Messaging.Contracts.Messages.Enums;

/// <summary>
/// Why a FlexForms application projection was requested.
/// </summary>
public enum ProjectionReason
{
    Saved,      // A new response version was saved
    Submitted,  // The application was submitted
    Deleted,    // The application was (soft) deleted
    Resync      // Backfill or reconciliation asked for the current state to be re-projected
}

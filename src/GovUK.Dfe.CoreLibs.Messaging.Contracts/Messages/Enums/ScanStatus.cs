namespace GovUK.Dfe.CoreLibs.Messaging.Contracts.Messages.Enums;

public enum ScanStatus
{
    Accepted,   // Scanner has received the job
    Completed,  // Scan finished successfully
    Failed,     // Scan failed (e.g. vendor error)
    TimedOut    // Scan didn't complete in time
}
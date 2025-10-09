namespace GovUK.Dfe.CoreLibs.Email.Models;

/// <summary>
/// Represents the status of an email
/// </summary>
public enum EmailStatus
{
    /// <summary>
    /// Email is queued for sending
    /// </summary>
    Queued,

    /// <summary>
    /// Email is being sent
    /// </summary>
    Sending,

    /// <summary>
    /// Email has been sent successfully
    /// </summary>
    Sent,

    /// <summary>
    /// Email was delivered successfully
    /// </summary>
    Delivered,

    /// <summary>
    /// Email delivery failed permanently
    /// </summary>
    PermanentFailure,

    /// <summary>
    /// Email delivery failed temporarily
    /// </summary>
    TemporaryFailure,

    /// <summary>
    /// Technical failure occurred
    /// </summary>
    TechnicalFailure,

    /// <summary>
    /// Email was accepted by the provider
    /// </summary>
    Accepted,

    /// <summary>
    /// Email was created but not yet processed
    /// </summary>
    Created,

    /// <summary>
    /// Unknown status
    /// </summary>
    Unknown
}

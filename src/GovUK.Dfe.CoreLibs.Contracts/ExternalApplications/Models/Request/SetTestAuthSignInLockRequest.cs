namespace GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request;

/// <summary>
/// Sets or clears an administrator's lock on a user's Test Authentication sign-in in the current tenant.
/// Clearing it also clears any temporary lockout from wrong codes.
/// </summary>
/// <param name="IsLocked"><c>true</c> to lock the user out of every sign-in method until an administrator unlocks them.</param>
public sealed record SetTestAuthSignInLockRequest(bool IsLocked);

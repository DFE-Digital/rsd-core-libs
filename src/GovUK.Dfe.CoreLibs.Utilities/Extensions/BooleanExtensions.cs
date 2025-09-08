namespace GovUK.Dfe.CoreLibs.Utilities.Extensions;

/// <summary>
/// The boolean extensions.
/// </summary>
public static class BooleanExtensions
{
    /// <summary>
    /// Returns a boolean value as Yes or No strings
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string ToYesNoString(this bool value)
    {
        return value ? "Yes" : "No";
    }

    /// <summary>
    /// Returns a boolean value as Yes or No strings
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string ToYesNoString(this bool? value)
    {
        if (value.HasValue)
        {
            return value.Value ? "Yes" : "No";
        }

        return "";
    }

    /// <summary>
    /// Returns a boolean value as Deficit or Surplus strings
    /// TRUE = Deficit
    /// FALSE = Surplus
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string ToSurplusDeficitString(this bool value)
    {
        return value ? "Deficit" : "Surplus";
    }

    /// <summary>
    /// Returns a boolean value as Deficit or Surplus strings
    /// TRUE = Deficit
    /// FALSE = Surplus
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string ToSurplusDeficitString(this bool? value)
    {
        if (value.HasValue)
        {
            return value.Value ? "Deficit" : "Surplus";
        }

        return string.Empty;
    }
}

namespace DfE.CoreLibs.Utilities.Extensions;

/// <summary>
/// The date time extensions.
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Returns a date in standard UK format (dd/MM/yyyy)
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static string ToUkDateString(this DateTime dateTime)
    {
        return dateTime.ToString("dd/MM/yyyy");
    }

    /// <summary>
    /// Returns a date string of either 'd MMMM yyyy' or 'dddd d MMMM yyyy' if includeDayOfWeek is true.
    /// </summary>
    /// <param name="dateTime"></param>
    /// <param name="includeDayOfWeek"></param>
    /// <returns></returns>
    public static string ToDateString(this DateTime? dateTime, bool includeDayOfWeek = false)
    {
        if (!dateTime.HasValue)
        {
            return string.Empty;
        }

        return ToDateString(dateTime.Value, includeDayOfWeek);
    }

    /// <summary>
    /// Returns a date string of either 'd MMMM yyyy' or 'dddd d MMMM yyyy' if includeDayOfWeek is true.
    /// </summary>
    /// <param name="dateTime"></param>
    /// <param name="includeDayOfWeek"></param>
    /// <returns></returns>
    public static string ToDateString(this DateTime dateTime, bool includeDayOfWeek = false)
    {
        if (includeDayOfWeek)
        {
            return dateTime.ToString("dddd d MMMM yyyy");
        }

        return dateTime.ToString("d MMMM yyyy");
    }

    /// <summary>
    /// Returns the date that is the first of the month, from the given month
    /// </summary>
    /// <param name="thisMonth"></param>
    /// <param name="monthsToAdd"></param>
    /// <returns></returns>
    public static DateTime FirstOfMonth(this DateTime thisMonth, int monthsToAdd = 0)
    {
        int month = (thisMonth.Month + monthsToAdd) % 12;
        if (month == 0)
        {
            month = 12;
        }

        int yearsToAdd = (thisMonth.Month + monthsToAdd - 1) / 12;
        return new DateTime(thisMonth.Year + yearsToAdd, month, 1);
    }
}
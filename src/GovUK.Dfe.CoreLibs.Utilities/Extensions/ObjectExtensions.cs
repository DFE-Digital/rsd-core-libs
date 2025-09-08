namespace GovUK.Dfe.CoreLibs.Utilities.Extensions;

/// <summary>
/// The object extensions.
/// </summary>
public static class ObjectExtensions
{
    /// <summary>
    /// Returns to string from an object if the instance is not null. Returns default if the object isnull
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="default"></param>
    /// <returns></returns>
    public static string? ToStringOrDefault(this object? obj, string? @default = null)
    {
        return obj?.ToString() ?? @default;
    }
}

namespace DfE.CoreLibs.Http.Utils;

/// <summary>
/// Utility class for generating error IDs.
/// </summary>
public static class ErrorIdGenerator
{
    private static readonly Random _random = new Random();
    private static readonly object _lock = new object();

    /// <summary>
    /// Generates a random 6-digit error ID.
    /// </summary>
    /// <returns>A 6-digit string representation of a random number.</returns>
    public static string GenerateDefault()
    {
        lock (_lock)
        {
            return _random.Next(100000, 999999).ToString();
        }
    }

    /// <summary>
    /// Generates a timestamp-based error ID with format: YYYYMMDD-HHMMSS-XXXX
    /// </summary>
    /// <returns>A timestamp-based error ID.</returns>
    public static string GenerateTimestampBased()
    {
        lock (_lock)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var random = _random.Next(1000, 9999);
            return $"{timestamp}-{random}";
        }
    }

    /// <summary>
    /// Generates a GUID-based error ID (first 8 characters).
    /// </summary>
    /// <returns>A GUID-based error ID.</returns>
    public static string GenerateGuidBased()
    {
        return Guid.NewGuid().ToString("N")[..8];
    }

    /// <summary>
    /// Generates a sequential error ID (not thread-safe, use with caution).
    /// </summary>
    /// <returns>A sequential error ID.</returns>
    public static string GenerateSequential()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return timestamp.ToString();
    }
} 
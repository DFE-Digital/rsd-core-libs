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
    /// Generates a random 6-digit error ID with environment prefix.
    /// </summary>
    /// <param name="environment">Environment prefix (e.g., "D" for Development, "T" for Test, "P" for Production)</param>
    /// <returns>An environment-prefixed 6-digit error ID.</returns>
    public static string GenerateDefault(string environment)
    {
        lock (_lock)
        {
            var randomId = _random.Next(100000, 999999).ToString();
            return $"{environment}-{randomId}";
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
    /// Generates a timestamp-based error ID with environment prefix.
    /// </summary>
    /// <param name="environment">Environment prefix (e.g., "D" for Development, "T" for Test, "P" for Production)</param>
    /// <returns>An environment-prefixed timestamp-based error ID.</returns>
    public static string GenerateTimestampBased(string environment)
    {
        lock (_lock)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var random = _random.Next(1000, 9999);
            return $"{environment}-{timestamp}-{random}";
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
    /// Generates a GUID-based error ID with environment prefix.
    /// </summary>
    /// <param name="environment">Environment prefix (e.g., "D" for Development, "T" for Test, "P" for Production)</param>
    /// <returns>An environment-prefixed GUID-based error ID.</returns>
    public static string GenerateGuidBased(string environment)
    {
        var guidId = Guid.NewGuid().ToString("N")[..8];
        return $"{environment}-{guidId}";
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

    /// <summary>
    /// Generates a sequential error ID with environment prefix.
    /// </summary>
    /// <param name="environment">Environment prefix (e.g., "D" for Development, "T" for Test, "P" for Production)</param>
    /// <returns>An environment-prefixed sequential error ID.</returns>
    public static string GenerateSequential(string environment)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return $"{environment}-{timestamp}";
    }

    /// <summary>
    /// Gets the appropriate environment prefix based on the environment name.
    /// </summary>
    /// <param name="environmentName">The environment name (case-insensitive).</param>
    /// <returns>The environment prefix.</returns>
    public static string GetEnvironmentPrefix(string environmentName)
    {
        return environmentName?.ToUpperInvariant() switch
        {
            "DEVELOPMENT" or "DEV" => "D",
            "TEST" or "STAGING" => "T",
            "PRODUCTION" or "PROD" => "P",
            "UAT" => "U",
            "QA" => "Q",
            _ => "X" // Unknown environment
        };
    }

    /// <summary>
    /// Generates a default error ID with environment prefix based on environment name.
    /// </summary>
    /// <param name="environmentName">The environment name (e.g., "Development", "Production").</param>
    /// <returns>An environment-prefixed error ID.</returns>
    public static string GenerateDefaultWithEnvironment(string environmentName)
    {
        var prefix = GetEnvironmentPrefix(environmentName);
        return GenerateDefault(prefix);
    }

    /// <summary>
    /// Generates a timestamp-based error ID with environment prefix based on environment name.
    /// </summary>
    /// <param name="environmentName">The environment name (e.g., "Development", "Production").</param>
    /// <returns>An environment-prefixed timestamp-based error ID.</returns>
    public static string GenerateTimestampBasedWithEnvironment(string environmentName)
    {
        var prefix = GetEnvironmentPrefix(environmentName);
        return GenerateTimestampBased(prefix);
    }

    /// <summary>
    /// Generates a GUID-based error ID with environment prefix based on environment name.
    /// </summary>
    /// <param name="environmentName">The environment name (e.g., "Development", "Production").</param>
    /// <returns>An environment-prefixed GUID-based error ID.</returns>
    public static string GenerateGuidBasedWithEnvironment(string environmentName)
    {
        var prefix = GetEnvironmentPrefix(environmentName);
        return GenerateGuidBased(prefix);
    }

    /// <summary>
    /// Generates a sequential error ID with environment prefix based on environment name.
    /// </summary>
    /// <param name="environmentName">The environment name (e.g., "Development", "Production").</param>
    /// <returns>An environment-prefixed sequential error ID.</returns>
    public static string GenerateSequentialWithEnvironment(string environmentName)
    {
        var prefix = GetEnvironmentPrefix(environmentName);
        return GenerateSequential(prefix);
    }
} 
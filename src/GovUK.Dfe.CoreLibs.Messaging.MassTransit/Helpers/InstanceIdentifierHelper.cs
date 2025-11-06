using Microsoft.Extensions.Configuration;

namespace GovUK.Dfe.CoreLibs.Messaging.MassTransit.Helpers;

/// <summary>
/// Helper class for managing instance identifiers in Local development environments.
/// Used to isolate message processing between multiple developers sharing the same Service Bus subscription.
/// </summary>
public static class InstanceIdentifierHelper
{
    /// <summary>
    /// Checks if the current environment is "Local" (developer machine).
    /// </summary>
    /// <returns>True if ASPNETCORE_ENVIRONMENT is set to "Local", false otherwise.</returns>
    public static bool IsLocalEnvironment()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        return string.Equals(environment, "Local", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the instance identifier for this service instance.
    /// Returns null if not in Local environment (no filtering needed).
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The instance identifier string, or null if not in Local environment.</returns>
    public static string? GetInstanceIdentifier(IConfiguration configuration)
    {
        // Only use instance identifiers in Local environment
        if (!IsLocalEnvironment())
            return null;

        var configValue = configuration["MassTransit:AzureServiceBus:InstanceIdentifier"];

        // If set to "auto", use machine name
        if (string.Equals(configValue, "auto", StringComparison.OrdinalIgnoreCase))
        {
            return Environment.MachineName;
        }

        // Otherwise use the configured value, or "default" if not set
        return configValue ?? "default";
    }

    /// <summary>
    /// Checks if a message's InstanceIdentifier matches the local instance.
    /// </summary>
    /// <param name="messageInstanceId">The InstanceIdentifier from the message metadata.</param>
    /// <param name="localInstanceId">The local instance identifier.</param>
    /// <returns>True if the message should be processed by this instance, false otherwise.</returns>
    public static bool IsMessageForThisInstance(string? messageInstanceId, string? localInstanceId)
    {
        // If no instance ID on message, process it (backwards compatible)
        if (string.IsNullOrEmpty(messageInstanceId))
        {
            return true;
        }

        // If "broadcast" mode, process it (allows overriding the filter)
        if (messageInstanceId.Equals("broadcast", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // If no local instance ID, process it
        if (string.IsNullOrEmpty(localInstanceId))
        {
            return true;
        }

        // Check if it matches this instance
        return messageInstanceId.Equals(localInstanceId, StringComparison.OrdinalIgnoreCase);
    }
}


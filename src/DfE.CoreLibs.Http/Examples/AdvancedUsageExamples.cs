using DfE.CoreLibs.Http.Configuration;
using DfE.CoreLibs.Http.Extensions;
using DfE.CoreLibs.Http.Interfaces;
using DfE.CoreLibs.Http.Models;
using DfE.CoreLibs.Http.Utils;

namespace DfE.CoreLibs.Http.Examples;

/// <summary>
/// Examples demonstrating advanced usage of the exception handler middleware.
/// </summary>
public static class AdvancedUsageExamples
{
    /// <summary>
    /// Example of using custom error ID generation.
    /// </summary>
    public static void CustomErrorIdGenerationExample()
    {
        // Custom error ID generator
        var options = new ExceptionHandlerOptions()
            .WithCustomErrorIdGenerator(() => $"ERR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}".Substring(0, 15));

        // Or use built-in generators
        var timestampOptions = new ExceptionHandlerOptions()
            .WithTimestampBasedErrorIds(); // YYYYMMDD-HHMMSS-XXXX

        var guidOptions = new ExceptionHandlerOptions()
            .WithGuidBasedErrorIds(); // First 8 chars of GUID

        var sequentialOptions = new ExceptionHandlerOptions()
            .WithSequentialErrorIds(); // Unix timestamp
    }

    /// <summary>
    /// Example of environment-aware error ID generation.
    /// </summary>
    public static void EnvironmentAwareErrorIdGenerationExample()
    {
        // Environment-aware default error IDs
        var devOptions = new ExceptionHandlerOptions()
            .WithEnvironmentAwareErrorIds("Development"); // D-123456

        var testOptions = new ExceptionHandlerOptions()
            .WithEnvironmentAwareErrorIds("Test"); // T-123456

        var prodOptions = new ExceptionHandlerOptions()
            .WithEnvironmentAwareErrorIds("Production"); // P-123456

        // Environment-aware timestamp-based error IDs
        var timestampOptions = new ExceptionHandlerOptions()
            .WithEnvironmentAwareTimestampErrorIds("Development"); // D-20240115-143022-5678

        // Environment-aware GUID-based error IDs
        var guidOptions = new ExceptionHandlerOptions()
            .WithEnvironmentAwareGuidErrorIds("Production"); // P-a1b2c3d4

        // Environment-aware sequential error IDs
        var sequentialOptions = new ExceptionHandlerOptions()
            .WithEnvironmentAwareSequentialErrorIds("Test"); // T-1705321822123
    }

    /// <summary>
    /// Example of shared post-processing for metrics and notifications.
    /// </summary>
    public static void SharedPostProcessingExample()
    {
        var options = new ExceptionHandlerOptions()
            .WithSharedPostProcessing((exception, response) =>
            {
                // Increment error counter
                IncrementErrorCounter(response.StatusCode);

                // Send notification for critical errors
                if (response.StatusCode >= 500)
                {
                    SendCriticalErrorNotification(response);
                }

                // Log to external system
                LogToExternalSystem(exception, response);

                // Add custom context
                response.Context = new Dictionary<string, object>
                {
                    ["processedAt"] = DateTime.UtcNow,
                    ["environment"] = "production"
                };
            });
    }

    /// <summary>
    /// Example combining custom handlers with shared post-processing.
    /// </summary>
    public static void CombinedUsageExample()
    {
        var options = new ExceptionHandlerOptions
        {
            IncludeDetails = false,
            LogExceptions = true,
            CustomHandlers = new List<ICustomExceptionHandler>
            {
                new BusinessRuleExceptionHandler(),
                new ValidationExceptionHandler()
            }
        }
        .WithTimestampBasedErrorIds()
        .WithSharedPostProcessing((exception, response) =>
        {
            // Common post-processing logic
            TrackErrorMetrics(response);
            NotifyTeamIfNeeded(response);
        });
    }

    /// <summary>
    /// Example of environment-specific configuration with environment-aware error IDs.
    /// </summary>
    public static void EnvironmentSpecificExample()
    {
        var isDevelopment = true; // Replace with actual environment check
        var environmentName = isDevelopment ? "Development" : "Production";

        var options = new ExceptionHandlerOptions
        {
            IncludeDetails = isDevelopment,
            LogExceptions = true,
            DefaultErrorMessage = isDevelopment ? "An error occurred" : "Something went wrong"
        }
        .WithEnvironmentAwareErrorIds(environmentName) // D-123456 or P-123456
        .WithSharedPostProcessing((exception, response) =>
        {
            if (isDevelopment)
            {
                // Development: Log to console
                Console.WriteLine($"DEV ERROR: {response.ErrorId} - {response.Message}");
            }
            else
            {
                // Production: Send to monitoring system
                SendToMonitoringSystem(response);
            }
        });
    }

    /// <summary>
    /// Example of different error ID strategies per environment.
    /// </summary>
    public static void EnvironmentSpecificErrorIdStrategiesExample()
    {
        var environmentName = "Development"; // Replace with actual environment

        var options = new ExceptionHandlerOptions
        {
            IncludeDetails = true,
            LogExceptions = true
        };

        // Different strategies for different environments
        switch (environmentName)
        {
            case "Development":
                options.WithEnvironmentAwareErrorIds(environmentName); // D-123456
                break;
            case "Test":
                options.WithEnvironmentAwareTimestampErrorIds(environmentName); // T-20240115-143022-5678
                break;
            case "Production":
                options.WithEnvironmentAwareGuidErrorIds(environmentName); // P-a1b2c3d4
                break;
            default:
                options.WithEnvironmentAwareSequentialErrorIds(environmentName); // X-1705321822123
                break;
        }
    }

    /// <summary>
    /// Example of using environment prefixes with custom logic.
    /// </summary>
    public static void CustomEnvironmentLogicExample()
    {
        var environmentName = "Development"; // Replace with actual environment
        var prefix = ErrorIdGenerator.GetEnvironmentPrefix(environmentName); // "D"

        var options = new ExceptionHandlerOptions()
            .WithCustomErrorIdGenerator(() => 
                $"{prefix}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}".Substring(0, 15));
    }

    // Helper methods for examples
    private static void IncrementErrorCounter(int statusCode)
    {
        // Implementation for metrics
    }

    private static void SendCriticalErrorNotification(ExceptionResponse response)
    {
        // Implementation for notifications
    }

    private static void LogToExternalSystem(Exception exception, ExceptionResponse response)
    {
        // Implementation for external logging
    }

    private static void TrackErrorMetrics(ExceptionResponse response)
    {
        // Implementation for metrics tracking
    }

    private static void NotifyTeamIfNeeded(ExceptionResponse response)
    {
        // Implementation for team notifications
    }

    private static void SendToMonitoringSystem(ExceptionResponse response)
    {
        // Implementation for monitoring
    }
} 
using DfE.CoreLibs.Http.Configuration;
using DfE.CoreLibs.Http.Extensions;
using DfE.CoreLibs.Http.Interfaces;
using DfE.CoreLibs.Http.Models;

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
} 
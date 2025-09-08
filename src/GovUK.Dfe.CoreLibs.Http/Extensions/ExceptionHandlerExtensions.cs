using GovUK.Dfe.CoreLibs.Http.Configuration;
using GovUK.Dfe.CoreLibs.Http.Interfaces;
using GovUK.Dfe.CoreLibs.Http.Middlewares.ExceptionHandler;
using GovUK.Dfe.CoreLibs.Http.Models;
using GovUK.Dfe.CoreLibs.Http.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GovUK.Dfe.CoreLibs.Http.Extensions;

/// <summary>
/// Extension methods for registering the global exception handler middleware.
/// </summary>
public static class ExceptionHandlerExtensions
{
    /// <summary>
    /// Adds the global exception handler middleware to the application pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetService<IOptions<ExceptionHandlerOptions>>()?.Value 
            ?? new ExceptionHandlerOptions();
        
        return app.UseMiddleware<GlobalExceptionHandlerMiddleware>(options);
    }

    /// <summary>
    /// Adds the global exception handler middleware to the application pipeline with custom options.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="configureOptions">Action to configure the exception handler options.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseGlobalExceptionHandler(
        this IApplicationBuilder app,
        Action<ExceptionHandlerOptions> configureOptions)
    {
        var options = new ExceptionHandlerOptions();
        configureOptions(options);
        
        return app.UseMiddleware<GlobalExceptionHandlerMiddleware>(options);
    }

    /// <summary>
    /// Adds the global exception handler middleware to the application pipeline with custom options.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="options">The exception handler options.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseGlobalExceptionHandler(
        this IApplicationBuilder app,
        ExceptionHandlerOptions options)
    {
        return app.UseMiddleware<GlobalExceptionHandlerMiddleware>(options);
    }

    /// <summary>
    /// Configures the global exception handler options in the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure the exception handler options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection ConfigureGlobalExceptionHandler(
        this IServiceCollection services,
        Action<ExceptionHandlerOptions> configureOptions)
    {
        services.Configure(configureOptions);
        return services;
    }

    /// <summary>
    /// Registers a custom exception handler with the global exception handler middleware.
    /// </summary>
    /// <typeparam name="THandler">The type of the custom exception handler.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCustomExceptionHandler<THandler>(this IServiceCollection services)
        where THandler : class, ICustomExceptionHandler
    {
        // Register both the concrete type and the interface
        services.AddScoped<THandler>();
        services.AddScoped<ICustomExceptionHandler, THandler>();
        return services;
    }

    /// <summary>
    /// Registers a custom exception handler with the global exception handler middleware.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="handler">The custom exception handler instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCustomExceptionHandler(
        this IServiceCollection services,
        ICustomExceptionHandler handler)
    {
        services.AddSingleton(handler);
        return services;
    }

    /// <summary>
    /// Registers multiple custom exception handlers with the global exception handler middleware.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="handlers">The custom exception handler instances.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCustomExceptionHandlers(
        this IServiceCollection services,
        params ICustomExceptionHandler[] handlers)
    {
        foreach (var handler in handlers)
        {
            services.AddSingleton(handler);
        }
        return services;
    }

    /// <summary>
    /// Configures the exception handler to use a custom error ID generator.
    /// </summary>
    /// <param name="options">The exception handler options.</param>
    /// <param name="errorIdGenerator">The custom error ID generator function.</param>
    /// <returns>The exception handler options for chaining.</returns>
    public static ExceptionHandlerOptions WithCustomErrorIdGenerator(
        this ExceptionHandlerOptions options,
        Func<string> errorIdGenerator)
    {
        options.ErrorIdGenerator = errorIdGenerator;
        return options;
    }

    /// <summary>
    /// Configures the exception handler to use a timestamp-based error ID generator.
    /// </summary>
    /// <param name="options">The exception handler options.</param>
    /// <returns>The exception handler options for chaining.</returns>
    public static ExceptionHandlerOptions WithTimestampBasedErrorIds(this ExceptionHandlerOptions options)
    {
        options.ErrorIdGenerator = ErrorIdGenerator.GenerateTimestampBased;
        return options;
    }

    /// <summary>
    /// Configures the exception handler to use a GUID-based error ID generator.
    /// </summary>
    /// <param name="options">The exception handler options.</param>
    /// <returns>The exception handler options for chaining.</returns>
    public static ExceptionHandlerOptions WithGuidBasedErrorIds(this ExceptionHandlerOptions options)
    {
        options.ErrorIdGenerator = ErrorIdGenerator.GenerateGuidBased;
        return options;
    }

    /// <summary>
    /// Configures the exception handler to use a sequential error ID generator.
    /// </summary>
    /// <param name="options">The exception handler options.</param>
    /// <returns>The exception handler options for chaining.</returns>
    public static ExceptionHandlerOptions WithSequentialErrorIds(this ExceptionHandlerOptions options)
    {
        options.ErrorIdGenerator = ErrorIdGenerator.GenerateSequential;
        return options;
    }

    /// <summary>
    /// Configures the exception handler to use environment-aware default error IDs.
    /// </summary>
    /// <param name="options">The exception handler options.</param>
    /// <param name="environmentName">The environment name (e.g., "Development", "Production").</param>
    /// <returns>The exception handler options for chaining.</returns>
    public static ExceptionHandlerOptions WithEnvironmentAwareErrorIds(
        this ExceptionHandlerOptions options,
        string environmentName)
    {
        options.ErrorIdGenerator = () => ErrorIdGenerator.GenerateDefaultWithEnvironment(environmentName);
        return options;
    }

    /// <summary>
    /// Configures the exception handler to use environment-aware timestamp-based error IDs.
    /// </summary>
    /// <param name="options">The exception handler options.</param>
    /// <param name="environmentName">The environment name (e.g., "Development", "Production").</param>
    /// <returns>The exception handler options for chaining.</returns>
    public static ExceptionHandlerOptions WithEnvironmentAwareTimestampErrorIds(
        this ExceptionHandlerOptions options,
        string environmentName)
    {
        options.ErrorIdGenerator = () => ErrorIdGenerator.GenerateTimestampBasedWithEnvironment(environmentName);
        return options;
    }

    /// <summary>
    /// Configures the exception handler to use environment-aware GUID-based error IDs.
    /// </summary>
    /// <param name="options">The exception handler options.</param>
    /// <param name="environmentName">The environment name (e.g., "Development", "Production").</param>
    /// <returns>The exception handler options for chaining.</returns>
    public static ExceptionHandlerOptions WithEnvironmentAwareGuidErrorIds(
        this ExceptionHandlerOptions options,
        string environmentName)
    {
        options.ErrorIdGenerator = () => ErrorIdGenerator.GenerateGuidBasedWithEnvironment(environmentName);
        return options;
    }

    /// <summary>
    /// Configures the exception handler to use environment-aware sequential error IDs.
    /// </summary>
    /// <param name="options">The exception handler options.</param>
    /// <param name="environmentName">The environment name (e.g., "Development", "Production").</param>
    /// <returns>The exception handler options for chaining.</returns>
    public static ExceptionHandlerOptions WithEnvironmentAwareSequentialErrorIds(
        this ExceptionHandlerOptions options,
        string environmentName)
    {
        options.ErrorIdGenerator = () => ErrorIdGenerator.GenerateSequentialWithEnvironment(environmentName);
        return options;
    }

    /// <summary>
    /// Configures shared post-processing action that will be executed after any handler processes an exception.
    /// </summary>
    /// <param name="options">The exception handler options.</param>
    /// <param name="postProcessingAction">The shared post-processing action.</param>
    /// <returns>The exception handler options for chaining.</returns>
    public static ExceptionHandlerOptions WithSharedPostProcessing(
        this ExceptionHandlerOptions options,
        Action<Exception, ExceptionResponse> postProcessingAction)
    {
        options.SharedPostProcessingAction = postProcessingAction;
        return options;
    }
} 

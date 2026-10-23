using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Integration.Fakes;

internal sealed record CollectedLog(string Category, LogLevel Level, string Message, Exception? Exception);

/// <summary>Captures every log entry written through DI-resolved loggers, for asserting on warnings.</summary>
internal sealed class CollectingLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<CollectedLog> _logs = new();

    public IReadOnlyList<CollectedLog> Logs => [.. _logs];

    public IReadOnlyList<CollectedLog> AtLevel(LogLevel level) => [.. _logs.Where(log => log.Level == level)];

    public ILogger CreateLogger(string categoryName) => new CollectingLogger(categoryName, _logs);

    public void Dispose()
    {
        // Nothing to release; the collected entries stay readable after the container is disposed.
    }

    private sealed class CollectingLogger(string category, ConcurrentQueue<CollectedLog> logs) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
            => logs.Enqueue(new CollectedLog(category, logLevel, formatter(state, exception), exception));
    }
}

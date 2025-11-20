namespace GovUK.Dfe.CoreLibs.AsyncProcessing.Models
{
    internal sealed class TaskWorkItem
    {
        public required Func<CancellationToken, Task> ExecuteAsync { get; init; }
        public required Type TaskType { get; init; }
        public CancellationToken? CallerToken { get; init; }
        public DateTime EnqueuedAt { get; init; } = DateTime.UtcNow;
    }
}


namespace GovUK.Dfe.CoreLibs.AsyncProcessing.Configurations
{
    public sealed class BackgroundServiceOptions
    {
        /// <summary>
        /// If true, each queued task will receive the global stopping token so it can be canceled 
        /// when the app shuts down. 
        /// If false, tasks will run to completion regardless of shutdown signals (i.e. CancellationToken.None).
        /// </summary>
        public bool UseGlobalStoppingToken { get; set; } = false;

        /// <summary>
        /// Maximum number of concurrent workers processing tasks from the channel.
        /// Default is 1 (sequential processing). Set higher for parallel processing.
        /// </summary>
        public int MaxConcurrentWorkers { get; set; } = 1;

        /// <summary>
        /// Maximum capacity of the task channel. When full, EnqueueTask will block or fail based on ChannelFullMode.
        /// Default is unbounded (int.MaxValue).
        /// </summary>
        public int ChannelCapacity { get; set; } = int.MaxValue;

        /// <summary>
        /// Behavior when channel is full: Wait (block until space), DropOldest, or ThrowException.
        /// Default is Wait.
        /// </summary>
        public ChannelFullMode ChannelFullMode { get; set; } = ChannelFullMode.Wait;

        /// <summary>
        /// Enable detailed logging for diagnostics. Disable in production for better performance.
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = false;
    }

    public enum ChannelFullMode
    {
        Wait,
        DropOldest,
        ThrowException
    }
}

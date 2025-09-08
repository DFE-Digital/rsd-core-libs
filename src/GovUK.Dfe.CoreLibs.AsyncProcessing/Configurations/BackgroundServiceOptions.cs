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
    }
}

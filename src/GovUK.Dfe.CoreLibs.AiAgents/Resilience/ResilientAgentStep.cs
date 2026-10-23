namespace GovUK.Dfe.CoreLibs.AiAgents.Resilience;
/// <summary>
/// Provides methods for executing steps with resilience, allowing for fallback results when exceptions occur.
/// </summary>
public static class ResilientAgentStep
{
    /// <summary>
    /// Executes a step and returns a fallback result when a suppressible exception occurs.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="step">The step to execute.</param>
    /// <param name="fallback">Creates the fallback result.</param>
    /// <param name="shouldSuppress">Determines which exceptions to suppress.</param>
    /// <returns>The step result or fallback result.</returns>
    public static async Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> step, Func<Exception, TResult> fallback, Func<Exception, bool>? shouldSuppress = null)
    {
        try
        {
            return await step().ConfigureAwait(false);
        }
        catch (Exception ex) when (
            ex is not OperationCanceledException &&
            (shouldSuppress is null || shouldSuppress(ex)))
        {
            return fallback(ex);
        }
    }
}
using GovUK.Dfe.CoreLibs.AiAgents.Constants;
using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

namespace GovUK.Dfe.CoreLibs.AiAgents.Extensions;

public static class AgentStepResultExtensions
{
    /// <summary>
    /// Converts an <see cref="AgentStepResult"/> to an <see cref="AgentResult"/>, providing a default failure message if the result is null.
    /// </summary>
    /// <param name="step"></param>
    /// <param name="failureMessage"></param>
    /// <returns></returns>
    /// <remarks>
    /// A failed step still reports the tokens its run used before failing (e.g. tool-call rounds before
    /// a timeout), because Foundry billed them - so briefing totals don't undercount failures.
    /// </remarks>
    public static AgentResult ToAgentResult(this AgentStepResult step,
        string failureMessage = ErrorMessages.UnableToGenerateSection)
    {
        if (step.Result is not null)
        {
            return step.Result;
        }

        var usage = Diagnostics.AgentTelemetry.TokenUsageOf(step.Error);
        return new AgentResult(step.AgentName, failureMessage, usage.TotalTokens)
        {
            InputTokens = usage.InputTokens,
            OutputTokens = usage.OutputTokens,
        };
    }
}

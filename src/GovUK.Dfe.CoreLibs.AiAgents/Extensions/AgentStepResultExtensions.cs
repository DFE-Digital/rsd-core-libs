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
    public static AgentResult ToAgentResult(this AgentStepResult step,
        string failureMessage = ErrorMessages.UnableToGenerateSection)
        => step.Result ?? new AgentResult(step.AgentName, failureMessage, TotalTokens: 0);
}

using GovUK.Dfe.CoreLibs.AiAgents.ValueObjects;

namespace GovUK.Dfe.CoreLibs.AiAgents.Factories;

/// <summary>
/// Options for configuring the Foundry agent factory.
/// </summary>
/// <param name="DefaultModel">
/// The default model deployment used when <see cref="AgentSpec.ModelOverride"/> is not set.
/// </param>
public sealed record FoundryAgentFactoryOptions(string DefaultModel);

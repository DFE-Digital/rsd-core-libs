using Azure.AI.Projects.Agents;
using Azure.Core;
using GovUK.Dfe.CoreLibs.AiAgents.Agents.Interfaces;
using GovUK.Dfe.CoreLibs.AiAgents.Concurrency;
using GovUK.Dfe.CoreLibs.AiAgents.Factories;
using GovUK.Dfe.CoreLibs.AiAgents.Factories.Interfaces;
using Microsoft.Extensions.Logging;

namespace GovUK.Dfe.CoreLibs.AiAgents.Agents;

/// <summary>
/// The <c>ExternallyManagedAgents</c> project, when <c>Endpoint</c> is set. Agents run in the project that owns them, so it
/// has its own factory and runner, sharing this app's run limits.
/// </summary>
internal sealed class ExternalFoundryProject
{
    public ExternalFoundryProject(TokenCredential credential, AgentAdministrationClient admin,
        IFoundryConversationClient conversations, FoundryAgentFactoryOptions factoryOptions, AgentRunOptions runOptions,
        IAgentRunLimiter runLimiter, ILoggerFactory loggers)
    {
        Credential = credential;
        Factory = new FoundryAgentFactory(admin, factoryOptions, loggers.CreateLogger<FoundryAgentFactory>());
        Runner = new FoundryAgentRunner(Factory, conversations, loggers.CreateLogger<FoundryAgentRunner>(), runOptions, runLimiter);
    }

    /// <summary>Its own <c>Authentication</c>, or this app's Foundry credential.</summary>
    public TokenCredential Credential { get; }

    public IAgentFactory Factory { get; }

    public IAgentRunner Runner { get; }
}

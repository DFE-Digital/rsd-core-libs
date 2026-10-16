namespace GovUK.Dfe.CoreLibs.AiAgents;

/// <summary>The Azure services the library signs in to, for <see cref="AiAgentsBuilder.UseCredentialFor"/>.</summary>
public enum AiAgentsService
{
    Foundry,
    Search,

    /// <summary>The <c>GlobalConcurrency</c> run-slot blob container.</summary>
    RunSlots,
}

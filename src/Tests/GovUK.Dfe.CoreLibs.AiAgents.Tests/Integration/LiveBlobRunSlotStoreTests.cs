using Azure.Identity;
using Azure.Storage.Blobs;
using GovUK.Dfe.CoreLibs.AiAgents.Concurrency;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Integration;

/// <summary>Runs only when <c>AIAGENTS_LIVE_SLOT_CONTAINER</c> is set to a blob container URI used for nothing else.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class LiveBlobFactAttribute : FactAttribute
{
    public LiveBlobFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(LiveBlobRunSlotStoreTests.ContainerVariable)))
        {
            Skip = $"Set {LiveBlobRunSlotStoreTests.ContainerVariable} to run against a real blob container.";
        }
    }
}

public sealed class LiveBlobRunSlotStoreTests
{
    public const string ContainerVariable = "AIAGENTS_LIVE_SLOT_CONTAINER";

    [LiveBlobFact]
    public async Task ASlot_IsHeldUntilReleased_ThenFreeForTheNextRun()
    {
        var container = new BlobContainerClient(new Uri(Environment.GetEnvironmentVariable(ContainerVariable)!), new DefaultAzureCredential());
        var store = new BlobRunSlotStore(container, capacity: 1);

        var held = await store.TryAcquireAsync(0);
        Assert.NotNull(held);
        Assert.Null(await store.TryAcquireAsync(0));   // a second instance can't take it

        await held.DisposeAsync();
        var next = await store.TryAcquireAsync(0);
        Assert.NotNull(next);
        await next.DisposeAsync();
    }
}

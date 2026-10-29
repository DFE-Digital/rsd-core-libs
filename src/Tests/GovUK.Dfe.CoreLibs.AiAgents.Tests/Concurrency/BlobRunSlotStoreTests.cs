using Azure.Core;
using Azure.Storage.Blobs.Models;
using GovUK.Dfe.CoreLibs.AiAgents.Concurrency;
using GovUK.Dfe.CoreLibs.AiAgents.Tests.Integration.Fakes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace GovUK.Dfe.CoreLibs.AiAgents.Tests.Concurrency;

/// <summary>The <c>GlobalConcurrency</c> run slot container: created if missing, checked at startup, used only by Entra ID.</summary>
public sealed class BlobRunSlotStoreTests
{
    private readonly InMemoryBlobContainer _container = new();
    private readonly CollectingLoggerProvider _logs = new();

    private BlobRunSlotStore Store(int capacity = 2) => new(_container, capacity);

    private RunSlotStartupValidator StartupCheck()
    {
        var loggers = LoggerFactory.Create(builder => builder.AddProvider(_logs));
        return new RunSlotStartupValidator(Store(), loggers.CreateLogger<RunSlotStartupValidator>());
    }

    private static ServiceProvider Build(params (string Key, string Value)[] globalConcurrency)
    {
        var settings = new Dictionary<string, string?>
        {
            ["AiAgents:Foundry:Endpoint"] = "https://example.services.ai.azure.com/api/projects/test",
            ["AiAgents:Foundry:DefaultModel"] = "myconnection/gpt-5.1",
        };
        foreach (var (key, value) in globalConcurrency)
        {
            settings[$"AiAgents:GlobalConcurrency:{key}"] = value;
        }

        var services = new ServiceCollection();
        services.AddAiAgents(new ConfigurationBuilder().AddInMemoryCollection(settings).Build(),
            agents => agents.UseCredential(Substitute.For<TokenCredential>()));
        return services.BuildServiceProvider();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AtStartup_AMissingContainer_IsCreatedPrivate_AnExistingOneIsUsed_AndTheIdentitysAccessIsProved(bool exists)
    {
        _container.ContainerExists = exists;

        await StartupCheck().StartAsync(CancellationToken.None);

        Assert.True(_container.ContainerExists);
        Assert.Equal(exists ? null : PublicAccessType.None, _container.CreatedWith);
        Assert.Contains(BlobRunSlotStore.AccessCheckBlob, _container.BlobNames);
        Assert.False(_container.IsLeased(BlobRunSlotStore.AccessCheckBlob));   // the check's lease is released
    }

    [Fact]
    public async Task AnIdentityWithoutTheRole_FailsStartup_AndRuns_NamingTheRoleAndContainer()
    {
        _container.ContainerExists = true;
        _container.Denied = true;

        var atStartup = await Assert.ThrowsAsync<InvalidOperationException>(() => StartupCheck().StartAsync(CancellationToken.None));
        var atRun = await Assert.ThrowsAsync<InvalidOperationException>(() => Store().TryAcquireAsync(0));

        Assert.All(new[] { atStartup, atRun }, ex =>
        {
            Assert.Contains("Storage Blob Data Contributor", ex.Message, StringComparison.Ordinal);
            Assert.Contains(_container.Uri.ToString(), ex.Message, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task AtStartup_AnUnreachableAccount_OnlyWarns_SoAnOutageCantStopInstancesStarting()
    {
        _container.Unreachable = true;

        await StartupCheck().StartAsync(CancellationToken.None);

        Assert.Single(_logs.AtLevel(LogLevel.Warning));
    }

    [Fact]
    public async Task ASlot_IsHeldByOneRunAtATime_AndFreeAgainOnceReleased()
    {
        _container.ContainerExists = true;
        var store = Store();

        var held = await store.TryAcquireAsync(0);
        Assert.NotNull(held);
        Assert.Null(await Store().TryAcquireAsync(0));   // another instance can't take it

        await held.DisposeAsync();
        await using var next = await store.TryAcquireAsync(0);
        Assert.NotNull(next);
    }

    [Fact]
    public async Task AContainerDeletedAfterStartup_IsRecreated_AndTheRunGetsItsSlot()
    {
        var store = Store();
        await store.EnsureAccessAsync();
        _container.Delete();

        await using var held = await store.TryAcquireAsync(1);

        Assert.NotNull(held);
        Assert.True(_container.IsLeased("run-slot-0001"));
    }

    [Fact]
    public void WithGlobalConcurrency_TheStartupCheckIsRegistered_AndWithoutItNothingIs()
    {
        using var withLimit = Build(("MaxConcurrentRuns", "20"), ("BlobContainerUri", "https://account.blob.core.windows.net/aiagents-run-slots"));
        using var withoutLimit = Build();

        Assert.Equal(20, Assert.IsType<BlobRunSlotStore>(withLimit.GetRequiredService<IRunSlotStore>()).Capacity);
        Assert.Single(withLimit.GetServices<IHostedService>().OfType<RunSlotStartupValidator>());
        Assert.Empty(withoutLimit.GetServices<IHostedService>().OfType<RunSlotStartupValidator>());
        Assert.Null(withoutLimit.GetService<IRunSlotStore>());
    }

    [Theory]
    [InlineData("http://account.blob.core.windows.net/aiagents-run-slots")]                  // not encrypted
    [InlineData("https://account.blob.core.windows.net/aiagents-run-slots?sv=2024&sig=abc")]  // a SAS token
    public void AContainerUriThatIsntHttpsOrCarriesASasToken_FailsStartup(string uri)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Build(("MaxConcurrentRuns", "20"), ("BlobContainerUri", uri)));

        Assert.Contains("BlobContainerUri (must be https, with no SAS token)", ex.Message, StringComparison.Ordinal);
    }
}

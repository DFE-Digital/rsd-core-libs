using GovUK.Dfe.CoreLibs.Messaging.Contracts.Messages.Enums;
using GovUK.Dfe.CoreLibs.Messaging.Contracts.Messages.Identifiers;

namespace GovUK.Dfe.CoreLibs.Messaging.MassTransit.Tests.Contracts;

public class ApplicationProjectionIdentifiersTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ApplicationId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void DeterministicGuid_matches_rfc4122_version5_test_vector()
    {
        var dnsNamespace = Guid.Parse("6ba7b810-9dad-11d1-80b4-00c04fd430c8");

        DeterministicGuid.Create(dnsNamespace, "python.org")
            .Should().Be(Guid.Parse("886313e1-3b8a-5372-9b90-0c9aee199e5d"));
    }

    [Fact]
    public void MessageId_is_stable_for_the_same_inputs()
    {
        var first = ApplicationProjectionIdentifiers.MessageId(TenantId, ApplicationId, 7, ProjectionReason.Saved);
        var second = ApplicationProjectionIdentifiers.MessageId(TenantId, ApplicationId, 7, ProjectionReason.Saved);

        first.Should().Be(second);
    }

    [Theory]
    [InlineData(8, ProjectionReason.Saved)]
    [InlineData(7, ProjectionReason.Submitted)]
    [InlineData(7, ProjectionReason.Deleted)]
    public void MessageId_changes_with_revision_or_reason(long revision, ProjectionReason reason)
    {
        var baseline = ApplicationProjectionIdentifiers.MessageId(TenantId, ApplicationId, 7, ProjectionReason.Saved);

        ApplicationProjectionIdentifiers.MessageId(TenantId, ApplicationId, revision, reason)
            .Should().NotBe(baseline);
    }

    [Fact]
    public void Resync_message_ids_differ_per_operation()
    {
        var first = ApplicationProjectionIdentifiers.MessageId(TenantId, ApplicationId, 7, ProjectionReason.Resync, Guid.NewGuid());
        var second = ApplicationProjectionIdentifiers.MessageId(TenantId, ApplicationId, 7, ProjectionReason.Resync, Guid.NewGuid());

        first.Should().NotBe(second);
    }

    [Fact]
    public void Resync_message_id_requires_operation_id()
    {
        var act = () => ApplicationProjectionIdentifiers.MessageId(TenantId, ApplicationId, 7, ProjectionReason.Resync);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SessionId_combines_tenant_and_application()
    {
        ApplicationProjectionIdentifiers.SessionId(TenantId, ApplicationId)
            .Should().Be("11111111-1111-1111-1111-111111111111:22222222-2222-2222-2222-222222222222");
    }

    [Fact]
    public void SubmissionId_is_deterministic_and_revision_specific()
    {
        ApplicationProjectionIdentifiers.SubmissionId(ApplicationId, 5)
            .Should().Be(ApplicationProjectionIdentifiers.SubmissionId(ApplicationId, 5))
            .And.NotBe(ApplicationProjectionIdentifiers.SubmissionId(ApplicationId, 6));
    }
}

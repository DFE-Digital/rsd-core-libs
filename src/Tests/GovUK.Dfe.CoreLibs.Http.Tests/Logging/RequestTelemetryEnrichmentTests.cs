using FluentAssertions;
using GovUK.Dfe.CoreLibs.Http.Interfaces;
using GovUK.Dfe.CoreLibs.Http.Logging;
using GovUK.Dfe.CoreLibs.Http.Models;
using NSubstitute;

namespace GovUK.Dfe.CoreLibs.Http.Tests.Logging;

public class RequestTelemetryEnrichmentTests
{
    [Fact]
    public void ApplyToExceptionResponse_ShouldDoNothing_WhenTelemetryIsNull()
    {
        var response = new ExceptionResponse
        {
            TenantId = "existing-tenant",
            Context = new Dictionary<string, object> { ["keep"] = true }
        };

        RequestTelemetryEnrichment.ApplyToExceptionResponse(response, null);

        response.TenantId.Should().Be("existing-tenant");
        response.Context.Should().ContainKey("keep");
        response.Context.Should().HaveCount(1);
    }

    [Fact]
    public void ApplyToExceptionResponse_ShouldPopulateMissingFieldsFromTelemetry()
    {
        var telemetry = Substitute.For<IRequestTelemetryContext>();
        telemetry.TenantId.Returns("tenant-1");
        telemetry.TenantName.Returns("Tenant One");
        telemetry.UserEmail.Returns("user@example.com");
        telemetry.CorrelationId.Returns("550e8400-e29b-41d4-a716-446655440000");
        telemetry.UserId.Returns("user-123");
        telemetry.ServiceName.Returns("example-api");

        var response = new ExceptionResponse();

        RequestTelemetryEnrichment.ApplyToExceptionResponse(response, telemetry);

        response.TenantId.Should().Be("tenant-1");
        response.TenantName.Should().Be("Tenant One");
        response.UserEmail.Should().Be("user@example.com");
        response.CorrelationId.Should().Be("550e8400-e29b-41d4-a716-446655440000");
        response.Context.Should().NotBeNull();
        response.Context![LogContextKeys.UserId].Should().Be("user-123");
        response.Context[LogContextKeys.ServiceName].Should().Be("example-api");
    }

    [Fact]
    public void ApplyToExceptionResponse_ShouldNotOverwriteExistingResponseValues()
    {
        var telemetry = Substitute.For<IRequestTelemetryContext>();
        telemetry.TenantId.Returns("telemetry-tenant");
        telemetry.TenantName.Returns("Telemetry Tenant");
        telemetry.UserEmail.Returns("telemetry@example.com");
        telemetry.CorrelationId.Returns("telemetry-correlation");
        telemetry.UserId.Returns("telemetry-user");
        telemetry.ServiceName.Returns("telemetry-service");

        var response = new ExceptionResponse
        {
            TenantId = "response-tenant",
            TenantName = "Response Tenant",
            UserEmail = "response@example.com",
            CorrelationId = "response-correlation",
            Context = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                [LogContextKeys.UserId] = "response-user",
                [LogContextKeys.ServiceName] = "response-service"
            }
        };

        RequestTelemetryEnrichment.ApplyToExceptionResponse(response, telemetry);

        response.TenantId.Should().Be("response-tenant");
        response.TenantName.Should().Be("Response Tenant");
        response.UserEmail.Should().Be("response@example.com");
        response.CorrelationId.Should().Be("response-correlation");
        response.Context![LogContextKeys.UserId].Should().Be("response-user");
        response.Context[LogContextKeys.ServiceName].Should().Be("response-service");
    }

    [Fact]
    public void ApplyToExceptionResponse_ShouldMergeOnlyNonEmptyContextValues()
    {
        var telemetry = Substitute.For<IRequestTelemetryContext>();
        telemetry.UserId.Returns("   ");
        telemetry.ServiceName.Returns("flexforms-api");

        var response = new ExceptionResponse
        {
            Context = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                [LogContextKeys.ServiceName] = "existing-service"
            }
        };

        RequestTelemetryEnrichment.ApplyToExceptionResponse(response, telemetry);

        response.Context.Should().NotContainKey(LogContextKeys.UserId);
        response.Context![LogContextKeys.ServiceName].Should().Be("existing-service");
    }

    [Fact]
    public void ApplyToExceptionResponse_ShouldUseExistingContextDictionary_WhenPresent()
    {
        var telemetry = Substitute.For<IRequestTelemetryContext>();
        telemetry.UserId.Returns("user-42");
        telemetry.ServiceName.Returns("api");

        var response = new ExceptionResponse
        {
            Context = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["custom"] = "value"
            }
        };

        RequestTelemetryEnrichment.ApplyToExceptionResponse(response, telemetry);

        response.Context.Should().ContainKey("custom");
        response.Context![LogContextKeys.UserId].Should().Be("user-42");
        response.Context[LogContextKeys.ServiceName].Should().Be("api");
    }
}

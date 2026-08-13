using FluentAssertions;
using GovUK.Dfe.CoreLibs.Http.Extensions;
using GovUK.Dfe.CoreLibs.Http.Interfaces;
using GovUK.Dfe.CoreLibs.Http.Middlewares.CorrelationId;
using GovUK.Dfe.CoreLibs.Http.Middlewares.RequestTelemetry;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace GovUK.Dfe.CoreLibs.Http.Tests.Extensions;

public class CorrelationIdExtensionsTests
{
    [Fact]
    public void AddCorrelationId_ShouldRegisterCorrelationAndTelemetryServices()
    {
        var services = new ServiceCollection();

        var result = services.AddCorrelationId();

        result.Should().BeSameAs(services);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<ICorrelationContext>()
            .Should().BeOfType<CorrelationContext>();
        scope.ServiceProvider.GetRequiredService<IRequestTelemetryContext>()
            .Should().BeOfType<RequestTelemetryContext>();
    }

    [Fact]
    public void UseCorrelationId_ShouldRegisterMiddlewareAndReturnBuilder()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCorrelationId();
        var app = new ApplicationBuilder(services.BuildServiceProvider());

        var result = app.UseCorrelationId();

        result.Should().BeSameAs(app);
    }
}

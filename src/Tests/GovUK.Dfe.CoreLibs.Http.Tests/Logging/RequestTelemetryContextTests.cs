using GovUK.Dfe.CoreLibs.Http.Logging;
using GovUK.Dfe.CoreLibs.Http.Middlewares.RequestTelemetry;

namespace GovUK.Dfe.CoreLibs.Http.Tests.Logging;

public class RequestTelemetryContextTests
{
    [Fact]
    public void ToScopeDictionary_ShouldIncludeOnlyNonEmptyValues()
    {
        var context = new RequestTelemetryContext
        {
            CorrelationId = "550e8400-e29b-41d4-a716-446655440000",
            TenantId = "11111111-1111-4111-8111-111111111111",
            UserEmail = "user@example.com",
            TemplateId = "template-1"
        };

        var scope = context.ToScopeDictionary();

        Assert.Equal(4, scope.Count);
        Assert.Equal(context.CorrelationId, scope[LogContextKeys.CorrelationId]);
        Assert.Equal(context.TenantId, scope[LogContextKeys.TenantId]);
        Assert.Equal(context.UserEmail, scope[LogContextKeys.UserEmail]);
        Assert.Equal(context.TemplateId, scope[LogContextKeys.TemplateId]);
    }
}

using GovUK.Dfe.CoreLibs.SharePoint.Exceptions;

namespace GovUK.Dfe.CoreLibs.SharePoint.Tests;

public class SharePointExceptionTests
{
    [Fact]
    public void SharePointException_WithMessage_SetsMessage()
    {
        var ex = new SharePointException("error");
        Assert.Equal("error", ex.Message);
    }

    [Fact]
    public void SharePointNotFoundException_WithInnerException_PreservesInner()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new SharePointNotFoundException("not found", inner);

        Assert.Equal("not found", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void SharePointConfigurationException_DefaultConstructor_Succeeds()
    {
        var ex = new SharePointConfigurationException();
        Assert.NotNull(ex);
    }
}

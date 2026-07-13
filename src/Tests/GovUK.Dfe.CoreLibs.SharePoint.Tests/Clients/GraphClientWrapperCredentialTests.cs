using Azure.Identity;
using GovUK.Dfe.CoreLibs.SharePoint.Clients;
using GovUK.Dfe.CoreLibs.SharePoint.Settings;

namespace GovUK.Dfe.CoreLibs.SharePoint.Tests.Clients;

public class GraphClientWrapperCredentialTests
{
    [Fact]
    public void CreateCredential_WithClientSecret_ReturnsClientSecretCredential()
    {
        var options = new SharePointOptions
        {
            TenantId = "tenant",
            ClientId = "client",
            ClientSecret = "secret"
        };

        var credential = GraphClientWrapper.CreateCredential(options);

        Assert.IsType<ClientSecretCredential>(credential);
    }

    [Fact]
    public void CreateCredential_WithMissingCertificateFile_Throws()
    {
        var options = new SharePointOptions
        {
            TenantId = "tenant",
            ClientId = "client",
            CertificatePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".pfx")
        };

        Assert.ThrowsAny<Exception>(() => GraphClientWrapper.CreateCredential(options));
    }
}

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Azure.Identity;
using GovUK.Dfe.CoreLibs.SharePoint.Clients;
using GovUK.Dfe.CoreLibs.SharePoint.Settings;
using Microsoft.Graph;

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

    [Fact]
    public void CreateCredential_WithCertificateFile_ReturnsClientCertificateCredential()
    {
        var path = WriteTempPfx(password: null);
        try
        {
            var options = new SharePointOptions
            {
                TenantId = "tenant",
                ClientId = "client",
                CertificatePath = path
            };

            var credential = GraphClientWrapper.CreateCredential(options);

            Assert.IsType<ClientCertificateCredential>(credential);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void CreateCredential_WithPasswordProtectedCertificate_ReturnsClientCertificateCredential()
    {
        const string password = "test-password";
        var path = WriteTempPfx(password);
        try
        {
            var options = new SharePointOptions
            {
                TenantId = "tenant",
                ClientId = "client",
                CertificatePath = path,
                CertificatePassword = password
            };

            var credential = GraphClientWrapper.CreateCredential(options);

            Assert.IsType<ClientCertificateCredential>(credential);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void CreateGraphClient_ReturnsGraphServiceClient()
    {
        var options = new SharePointOptions
        {
            TenantId = "tenant",
            ClientId = "client",
            ClientSecret = "secret"
        };

        var client = GraphClientWrapper.CreateGraphClient(options);

        Assert.NotNull(client);
        Assert.IsType<GraphServiceClient>(client);
    }

    [Fact]
    public void PublicConstructor_CreatesWrapperWithGraphClient()
    {
        var options = new SharePointOptions
        {
            TenantId = "tenant",
            ClientId = "client",
            ClientSecret = "secret",
            SiteId = "site-1"
        };

        var sut = new GraphClientWrapper(options);

        Assert.NotNull(sut);
    }

    private static string WriteTempPfx(string? password)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=GraphClientWrapperTests",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(1));

        var bytes = string.IsNullOrEmpty(password)
            ? certificate.Export(X509ContentType.Pfx)
            : certificate.Export(X509ContentType.Pfx, password);

        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".pfx");
        File.WriteAllBytes(path, bytes);
        return path;
    }
}

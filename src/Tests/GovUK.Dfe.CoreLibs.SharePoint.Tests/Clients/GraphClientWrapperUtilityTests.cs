using GovUK.Dfe.CoreLibs.SharePoint.Clients;
using GovUK.Dfe.CoreLibs.SharePoint.Exceptions;
using GovUK.Dfe.CoreLibs.SharePoint.Settings;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Serialization.Json;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace GovUK.Dfe.CoreLibs.SharePoint.Tests.Clients;

public class GraphClientWrapperUtilityTests
{
    [Theory]
    [InlineData("Documents", "Documents", "")]
    [InlineData("Documents/reports", "Documents", "reports")]
    [InlineData("Documents/reports/2024", "Documents", "reports/2024")]
    [InlineData("/Documents/reports/", "Documents", "reports")]
    [InlineData("Documents\\reports\\2024", "Documents", "reports/2024")]
    [InlineData("  Documents / reports  ", "Documents", "reports")]
    public void SplitLibraryAndPath_ParsesLibraryAndRelativePath(
        string folderPath,
        string expectedLibrary,
        string expectedRelativePath)
    {
        var (libraryName, relativePath) = GraphClientWrapper.SplitLibraryAndPath(folderPath);

        Assert.Equal(expectedLibrary, libraryName);
        Assert.Equal(expectedRelativePath, relativePath);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("/")]
    [InlineData("\\")]
    public void SplitLibraryAndPath_EmptyPath_Throws(string folderPath)
    {
        var ex = Assert.Throws<SharePointException>(() => GraphClientWrapper.SplitLibraryAndPath(folderPath));

        Assert.Contains("document library name", ex.Message);
    }

    [Fact]
    public async Task ResolveSiteIdAsync_WhenSiteIdConfigured_ReturnsConfiguredValue()
    {
        var options = CreateOptions(siteId: "configured-site-id");
        var requestAdapter = CreateRequestAdapter();
        var sut = new GraphClientWrapper(options, new GraphServiceClient(requestAdapter));

        var siteId = await sut.ResolveSiteIdAsync(CancellationToken.None);

        Assert.Equal("configured-site-id", siteId);
        await requestAdapter.DidNotReceiveWithAnyArgs()
            .SendAsync(
                Arg.Any<RequestInformation>(),
                Arg.Any<ParsableFactory<Site>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveSiteIdAsync_WhenSiteIdMissing_ResolvesFromHostnameAndPath()
    {
        var options = CreateOptions(siteId: "", siteHostname: "contoso.sharepoint.com", sitePath: "/sites/MySite");
        var requestAdapter = CreateRequestAdapter();
        requestAdapter
            .SendAsync(
                Arg.Any<RequestInformation>(),
                Arg.Any<ParsableFactory<Site>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
                Arg.Any<CancellationToken>())
            .Returns(new Site { Id = "resolved-site-id" });

        var sut = new GraphClientWrapper(options, new GraphServiceClient(requestAdapter));

        var siteId = await sut.ResolveSiteIdAsync(CancellationToken.None);

        Assert.Equal("resolved-site-id", siteId);
    }

    [Fact]
    public async Task ResolveSiteIdAsync_WhenSiteHasNoId_ThrowsConfigurationException()
    {
        var options = CreateOptions(siteId: "", siteHostname: "contoso.sharepoint.com", sitePath: "/sites/MySite");
        var requestAdapter = CreateRequestAdapter();
        requestAdapter
            .SendAsync(
                Arg.Any<RequestInformation>(),
                Arg.Any<ParsableFactory<Site>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
                Arg.Any<CancellationToken>())
            .Returns(new Site { Id = " " });

        var sut = new GraphClientWrapper(options, new GraphServiceClient(requestAdapter));

        var ex = await Assert.ThrowsAsync<SharePointConfigurationException>(
            () => sut.ResolveSiteIdAsync(CancellationToken.None));

        Assert.Contains("could not be resolved", ex.Message);
    }

    [Fact]
    public async Task ResolveSiteIdAsync_WhenSiteNotFound_ThrowsNotFoundException()
    {
        var options = CreateOptions(siteId: "", siteHostname: "contoso.sharepoint.com", sitePath: "/sites/Missing");
        var requestAdapter = CreateRequestAdapter();
        requestAdapter
            .SendAsync(
                Arg.Any<RequestInformation>(),
                Arg.Any<ParsableFactory<Site>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateNotFoundError());

        var sut = new GraphClientWrapper(options, new GraphServiceClient(requestAdapter));

        await Assert.ThrowsAsync<SharePointNotFoundException>(
            () => sut.ResolveSiteIdAsync(CancellationToken.None));
    }

    [Fact]
    public async Task ResolveDriveIdAsync_FindsLibraryByName_AndCachesResult()
    {
        var options = CreateOptions(siteId: "site-1");
        var requestAdapter = CreateRequestAdapter();
        requestAdapter
            .SendAsync(
                Arg.Any<RequestInformation>(),
                Arg.Any<ParsableFactory<DriveCollectionResponse>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
                Arg.Any<CancellationToken>())
            .Returns(new DriveCollectionResponse
            {
                Value =
                [
                    new Drive { Id = "drive-docs", Name = "Documents" },
                    new Drive { Id = "drive-other", Name = "Other" }
                ]
            });

        var sut = new GraphClientWrapper(options, new GraphServiceClient(requestAdapter));

        var first = await sut.ResolveDriveIdAsync("documents", CancellationToken.None);
        var second = await sut.ResolveDriveIdAsync("Documents", CancellationToken.None);

        Assert.Equal("drive-docs", first);
        Assert.Equal("drive-docs", second);
        await requestAdapter.Received(1).SendAsync(
            Arg.Any<RequestInformation>(),
            Arg.Any<ParsableFactory<DriveCollectionResponse>>(),
            Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveDriveIdAsync_WhenLibraryMissing_ThrowsNotFoundException()
    {
        var options = CreateOptions(siteId: "site-1");
        var requestAdapter = CreateRequestAdapter();
        requestAdapter
            .SendAsync(
                Arg.Any<RequestInformation>(),
                Arg.Any<ParsableFactory<DriveCollectionResponse>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
                Arg.Any<CancellationToken>())
            .Returns(new DriveCollectionResponse
            {
                Value = [new Drive { Id = "drive-other", Name = "Other" }]
            });

        var sut = new GraphClientWrapper(options, new GraphServiceClient(requestAdapter));

        var ex = await Assert.ThrowsAsync<SharePointNotFoundException>(
            () => sut.ResolveDriveIdAsync("Documents", CancellationToken.None));

        Assert.Contains("Documents", ex.Message);
    }

    [Fact]
    public async Task FolderExistsAsync_WhenFolderItemReturned_ReturnsTrue()
    {
        var options = CreateOptions(siteId: "site-1");
        var requestAdapter = CreateRequestAdapter();
        requestAdapter
            .SendAsync(
                Arg.Any<RequestInformation>(),
                Arg.Any<ParsableFactory<DriveItem>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
                Arg.Any<CancellationToken>())
            .Returns(new DriveItem { Name = "reports", Folder = new Folder() });

        var sut = new GraphClientWrapper(options, new GraphServiceClient(requestAdapter));

        var exists = await sut.FolderExistsAsync("drive-1", "reports", CancellationToken.None);

        Assert.True(exists);
    }

    [Fact]
    public async Task FolderExistsAsync_WhenItemIsFile_ReturnsFalse()
    {
        var options = CreateOptions(siteId: "site-1");
        var requestAdapter = CreateRequestAdapter();
        requestAdapter
            .SendAsync(
                Arg.Any<RequestInformation>(),
                Arg.Any<ParsableFactory<DriveItem>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
                Arg.Any<CancellationToken>())
            .Returns(new DriveItem { Name = "file.txt", File = new FileObject() });

        var sut = new GraphClientWrapper(options, new GraphServiceClient(requestAdapter));

        var exists = await sut.FolderExistsAsync("drive-1", "file.txt", CancellationToken.None);

        Assert.False(exists);
    }

    [Fact]
    public async Task FolderExistsAsync_WhenNotFound_ReturnsFalse()
    {
        var options = CreateOptions(siteId: "site-1");
        var requestAdapter = CreateRequestAdapter();
        requestAdapter
            .SendAsync(
                Arg.Any<RequestInformation>(),
                Arg.Any<ParsableFactory<DriveItem>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateNotFoundError());

        var sut = new GraphClientWrapper(options, new GraphServiceClient(requestAdapter));

        var exists = await sut.FolderExistsAsync("drive-1", "missing", CancellationToken.None);

        Assert.False(exists);
    }

    [Fact]
    public async Task FolderExistsAsync_WhenUnexpectedODataError_ThrowsSharePointException()
    {
        var options = CreateOptions(siteId: "site-1");
        var requestAdapter = CreateRequestAdapter();
        requestAdapter
            .SendAsync(
                Arg.Any<RequestInformation>(),
                Arg.Any<ParsableFactory<DriveItem>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new ODataError
            {
                Error = new MainError { Code = "accessDenied", Message = "denied" },
                ResponseStatusCode = 403
            });

        var sut = new GraphClientWrapper(options, new GraphServiceClient(requestAdapter));

        var ex = await Assert.ThrowsAsync<SharePointException>(
            () => sut.FolderExistsAsync("drive-1", "reports", CancellationToken.None));

        Assert.Contains("Failed to check whether folder", ex.Message);
    }

    private static SharePointOptions CreateOptions(
        string siteId = "site-1",
        string siteHostname = "",
        string sitePath = "") =>
        new()
        {
            TenantId = "tenant",
            ClientId = "client",
            ClientSecret = "secret",
            SiteId = siteId,
            SiteHostname = siteHostname,
            SitePath = sitePath
        };

    private static IRequestAdapter CreateRequestAdapter()
    {
        var requestAdapter = Substitute.For<IRequestAdapter>();
        requestAdapter.BaseUrl.Returns("https://graph.microsoft.com/v1.0");
        requestAdapter.SerializationWriterFactory.GetSerializationWriter(Arg.Any<string>())
            .Returns(_ => new JsonSerializationWriter());
        return requestAdapter;
    }

    private static ODataError CreateNotFoundError() =>
        new()
        {
            Error = new MainError { Code = "itemNotFound", Message = "Not found" },
            ResponseStatusCode = 404
        };
}

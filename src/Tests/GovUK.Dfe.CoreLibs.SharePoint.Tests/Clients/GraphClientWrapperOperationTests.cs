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

public class GraphClientWrapperOperationTests
{
    [Fact]
    public async Task CreateFolderAsync_LibraryRootOnly_ThrowsSharePointException()
    {
        var (sut, _) = CreateSutWithDrive("Documents");

        var ex = await Assert.ThrowsAsync<SharePointException>(
            () => sut.CreateFolderAsync("Documents"));

        Assert.Contains("Folder path must include a folder within the document library", ex.Message);
    }

    [Fact]
    public async Task CreateFolderAsync_CreatesTopLevelFolderUnderLibraryRoot()
    {
        var (sut, requestAdapter) = CreateSutWithDrive("Documents");

        requestAdapter
            .SendAsync(
                Arg.Is<RequestInformation>(r => r.HttpMethod == Method.GET),
                Arg.Any<ParsableFactory<DriveItem>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateNotFoundError());

        requestAdapter
            .SendAsync(
                Arg.Is<RequestInformation>(r => r.HttpMethod == Method.POST),
                Arg.Any<ParsableFactory<DriveItem>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
                Arg.Any<CancellationToken>())
            .Returns(new DriveItem { Name = "reports", Folder = new Folder() });

        await sut.CreateFolderAsync("Documents/reports");

        await requestAdapter.Received(1).SendAsync(
            Arg.Is<RequestInformation>(r => r.HttpMethod == Method.POST),
            Arg.Any<ParsableFactory<DriveItem>>(),
            Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateFolderAsync_CreatesMissingNestedFolders_SkipsExisting()
    {
        var (sut, requestAdapter) = CreateSutWithDrive("Documents");
        var getCount = 0;

        requestAdapter
            .SendAsync(
                Arg.Is<RequestInformation>(r => r.HttpMethod == Method.GET),
                Arg.Any<ParsableFactory<DriveItem>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
                Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                getCount++;
                // First segment already exists; deeper segment does not.
                if (getCount == 1)
                    return Task.FromResult<DriveItem?>(new DriveItem { Name = "reports", Folder = new Folder() });

                return Task.FromException<DriveItem?>(CreateNotFoundError());
            });

        requestAdapter
            .SendAsync(
                Arg.Is<RequestInformation>(r => r.HttpMethod == Method.POST),
                Arg.Any<ParsableFactory<DriveItem>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
                Arg.Any<CancellationToken>())
            .Returns(new DriveItem { Name = "2024", Folder = new Folder() });

        await sut.CreateFolderAsync("Documents/reports/2024");

        await requestAdapter.Received(1).SendAsync(
            Arg.Is<RequestInformation>(r => r.HttpMethod == Method.POST),
            Arg.Any<ParsableFactory<DriveItem>>(),
            Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
            Arg.Any<CancellationToken>());
        Assert.Equal(2, getCount);
    }

    [Fact]
    public async Task CreateFolderAsync_WhenConflictOnCreate_ContinuesWithoutThrowing()
    {
        var (sut, requestAdapter) = CreateSutWithDrive("Documents");

        requestAdapter
            .SendAsync(
                Arg.Is<RequestInformation>(r => r.HttpMethod == Method.GET),
                Arg.Any<ParsableFactory<DriveItem>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateNotFoundError());

        requestAdapter
            .SendAsync(
                Arg.Is<RequestInformation>(r => r.HttpMethod == Method.POST),
                Arg.Any<ParsableFactory<DriveItem>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new ODataError
            {
                Error = new MainError { Code = "nameAlreadyExists", Message = "exists" },
                ResponseStatusCode = 409
            });

        await sut.CreateFolderAsync("Documents/reports");
    }

    [Fact]
    public async Task CreateFolderAsync_WhenCreateFailsWithUnexpectedError_ThrowsSharePointException()
    {
        var (sut, requestAdapter) = CreateSutWithDrive("Documents");

        requestAdapter
            .SendAsync(
                Arg.Is<RequestInformation>(r => r.HttpMethod == Method.GET),
                Arg.Any<ParsableFactory<DriveItem>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateNotFoundError());

        requestAdapter
            .SendAsync(
                Arg.Is<RequestInformation>(r => r.HttpMethod == Method.POST),
                Arg.Any<ParsableFactory<DriveItem>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateAccessDeniedError());

        var ex = await Assert.ThrowsAsync<SharePointException>(
            () => sut.CreateFolderAsync("Documents/reports"));

        Assert.Contains("Failed to create folder 'Documents/reports'", ex.Message);
        Assert.Contains("denied", ex.Message);
    }

    [Fact]
    public async Task ListFilesAsync_ReturnsOnlyFiles_AndMapsMetadata()
    {
        var modified = new DateTimeOffset(2024, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var (sut, requestAdapter) = CreateSutWithDrive("Documents");

        requestAdapter
            .SendAsync(
                Arg.Any<RequestInformation>(),
                Arg.Any<ParsableFactory<DriveItemCollectionResponse>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
                Arg.Any<CancellationToken>())
            .Returns(new DriveItemCollectionResponse
            {
                Value =
                [
                    new DriveItem
                    {
                        Id = "file-1",
                        Name = "a.txt",
                        Size = 42,
                        LastModifiedDateTime = modified,
                        WebUrl = "https://contoso/a.txt",
                        File = new FileObject()
                    },
                    new DriveItem { Id = "folder-1", Name = "sub", Folder = new Folder() },
                    new DriveItem { Id = "file-2", Name = "", File = new FileObject() },
                    new DriveItem { Id = "file-3", Name = null, File = new FileObject() }
                ]
            });

        var files = await sut.ListFilesAsync("Documents/reports");

        var file = Assert.Single(files);
        Assert.Equal("file-1", file.Id);
        Assert.Equal("a.txt", file.Name);
        Assert.Equal(42, file.Size);
        Assert.Equal(modified, file.LastModified);
        Assert.Equal("https://contoso/a.txt", file.WebUrl);
    }

    [Fact]
    public async Task ListFilesAsync_WhenFolderMissing_ThrowsNotFoundException()
    {
        var (sut, requestAdapter) = CreateSutWithDrive("Documents");

        requestAdapter
            .SendAsync(
                Arg.Any<RequestInformation>(),
                Arg.Any<ParsableFactory<DriveItemCollectionResponse>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateNotFoundError());

        var ex = await Assert.ThrowsAsync<SharePointNotFoundException>(
            () => sut.ListFilesAsync("Documents/missing"));

        Assert.Contains("Folder 'Documents/missing' was not found", ex.Message);
    }

    [Fact]
    public async Task ListFilesAsync_WhenUnexpectedError_ThrowsSharePointException()
    {
        var (sut, requestAdapter) = CreateSutWithDrive("Documents");

        requestAdapter
            .SendAsync(
                Arg.Any<RequestInformation>(),
                Arg.Any<ParsableFactory<DriveItemCollectionResponse>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateAccessDeniedError());

        var ex = await Assert.ThrowsAsync<SharePointException>(
            () => sut.ListFilesAsync("Documents"));

        Assert.Contains("Failed to list files in folder 'Documents'", ex.Message);
    }

    [Fact]
    public async Task ListFilesAsync_FollowsNextLink_AndAggregatesPages()
    {
        var (sut, requestAdapter) = CreateSutWithDrive("Documents");
        var call = 0;

        requestAdapter
            .SendAsync(
                Arg.Any<RequestInformation>(),
                Arg.Any<ParsableFactory<DriveItemCollectionResponse>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                call++;
                if (call == 1)
                {
                    return new DriveItemCollectionResponse
                    {
                        Value =
                        [
                            new DriveItem { Id = "1", Name = "page1.txt", File = new FileObject() }
                        ],
                        OdataNextLink = "https://graph.microsoft.com/v1.0/drives/drive-1/root/children?$skiptoken=abc"
                    };
                }

                return new DriveItemCollectionResponse
                {
                    Value =
                    [
                        new DriveItem { Id = "2", Name = "page2.txt", File = new FileObject() }
                    ]
                };
            });

        var files = await sut.ListFilesAsync("Documents");

        Assert.Equal(2, files.Count);
        Assert.Equal(["page1.txt", "page2.txt"], files.Select(f => f.Name).ToArray());
        Assert.Equal(2, call);
    }

    [Fact]
    public async Task ListFilesAsync_NestedFolder_FollowsNextLinkViaItemPath()
    {
        var (sut, requestAdapter) = CreateSutWithDrive("Documents");
        var call = 0;

        requestAdapter
            .SendAsync(
                Arg.Any<RequestInformation>(),
                Arg.Any<ParsableFactory<DriveItemCollectionResponse>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                call++;
                if (call == 1)
                {
                    return new DriveItemCollectionResponse
                    {
                        Value =
                        [
                            new DriveItem { Id = "1", Name = "nested1.txt", File = new FileObject() }
                        ],
                        OdataNextLink =
                            "https://graph.microsoft.com/v1.0/drives/drive-1/root:/reports:/children?$skiptoken=xyz"
                    };
                }

                return new DriveItemCollectionResponse
                {
                    Value =
                    [
                        new DriveItem { Id = "2", Name = "nested2.txt", File = new FileObject() }
                    ]
                };
            });

        var files = await sut.ListFilesAsync("Documents/reports");

        Assert.Equal(["nested1.txt", "nested2.txt"], files.Select(f => f.Name).ToArray());
        Assert.Equal(2, call);
    }

    [Fact]
    public async Task ListFilesAsync_WhenNextPageFails_ThrowsSharePointException()
    {
        var (sut, requestAdapter) = CreateSutWithDrive("Documents");
        var call = 0;

        requestAdapter
            .SendAsync(
                Arg.Any<RequestInformation>(),
                Arg.Any<ParsableFactory<DriveItemCollectionResponse>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                call++;
                if (call == 1)
                {
                    return Task.FromResult<DriveItemCollectionResponse?>(new DriveItemCollectionResponse
                    {
                        Value =
                        [
                            new DriveItem { Id = "1", Name = "page1.txt", File = new FileObject() }
                        ],
                        OdataNextLink = "https://graph.microsoft.com/v1.0/drives/drive-1/root/children?$skiptoken=bad"
                    });
                }

                return Task.FromException<DriveItemCollectionResponse?>(CreateAccessDeniedError());
            });

        var ex = await Assert.ThrowsAsync<SharePointException>(
            () => sut.ListFilesAsync("Documents/reports"));

        Assert.Contains("Failed to list files in folder 'reports'", ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UploadFileAsync_InvalidFileName_ThrowsArgumentException(string? fileName)
    {
        var (sut, _) = CreateSutWithDrive("Documents");

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => sut.UploadFileAsync("Documents", fileName!, new MemoryStream([1])));
    }

    [Fact]
    public async Task UploadFileAsync_NullContent_ThrowsArgumentNullException()
    {
        var (sut, _) = CreateSutWithDrive("Documents");

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.UploadFileAsync("Documents", "a.txt", null!));
    }

    [Fact]
    public async Task UploadFileAsync_PutsContentAtLibraryRootPath()
    {
        var (sut, requestAdapter) = CreateSutWithDrive("Documents");
        RequestInformation? putRequest = null;

        requestAdapter
            .SendAsync(
                Arg.Any<RequestInformation>(),
                Arg.Any<ParsableFactory<DriveItem>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
                Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                putRequest = ci.ArgAt<RequestInformation>(0);
                Assert.Equal(Method.PUT, putRequest.HttpMethod);
                return new DriveItem { Id = "uploaded", Name = "a.txt", File = new FileObject() };
            });

        await using var content = new MemoryStream([1, 2, 3]);
        await sut.UploadFileAsync("Documents", "a.txt", content);

        Assert.NotNull(putRequest);
    }

    [Fact]
    public async Task UploadFileAsync_WhenFolderMissing_ThrowsNotFoundException()
    {
        var (sut, requestAdapter) = CreateSutWithDrive("Documents");

        requestAdapter
            .SendAsync(
                Arg.Any<RequestInformation>(),
                Arg.Any<ParsableFactory<DriveItem>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateNotFoundError());

        var ex = await Assert.ThrowsAsync<SharePointNotFoundException>(
            () => sut.UploadFileAsync("Documents/missing", "a.txt", new MemoryStream([1])));

        Assert.Contains("Folder 'Documents/missing' was not found", ex.Message);
    }

    [Fact]
    public async Task UploadFileAsync_WhenUnexpectedError_ThrowsSharePointException()
    {
        var (sut, requestAdapter) = CreateSutWithDrive("Documents");

        requestAdapter
            .SendAsync(
                Arg.Any<RequestInformation>(),
                Arg.Any<ParsableFactory<DriveItem>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateAccessDeniedError());

        var ex = await Assert.ThrowsAsync<SharePointException>(
            () => sut.UploadFileAsync("Documents/reports", "a.txt", new MemoryStream([1])));

        Assert.Contains("Failed to upload file 'Documents/reports/a.txt'", ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DownloadFileAsync_InvalidFileName_ThrowsArgumentException(string? fileName)
    {
        var (sut, _) = CreateSutWithDrive("Documents");

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => sut.DownloadFileAsync("Documents", fileName!));
    }

    [Fact]
    public async Task DownloadFileAsync_ReturnsContentStream()
    {
        var (sut, requestAdapter) = CreateSutWithDrive("Documents");
        var expected = new MemoryStream([9, 8, 7]);

        requestAdapter
            .SendPrimitiveAsync<Stream>(
                Arg.Any<RequestInformation>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
                Arg.Any<CancellationToken>())
            .Returns(expected);

        var stream = await sut.DownloadFileAsync("Documents/reports", "a.txt");

        Assert.Same(expected, stream);
    }

    [Fact]
    public async Task DownloadFileAsync_WhenStreamNull_ThrowsNotFoundException()
    {
        var (sut, requestAdapter) = CreateSutWithDrive("Documents");

        requestAdapter
            .SendPrimitiveAsync<Stream>(
                Arg.Any<RequestInformation>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
                Arg.Any<CancellationToken>())
            .Returns((Stream?)null);

        var ex = await Assert.ThrowsAsync<SharePointNotFoundException>(
            () => sut.DownloadFileAsync("Documents", "missing.txt"));

        Assert.Contains("File 'Documents/missing.txt' was not found", ex.Message);
    }

    [Fact]
    public async Task DownloadFileAsync_WhenNotFound_ThrowsNotFoundException()
    {
        var (sut, requestAdapter) = CreateSutWithDrive("Documents");

        requestAdapter
            .SendPrimitiveAsync<Stream>(
                Arg.Any<RequestInformation>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateNotFoundError());

        var ex = await Assert.ThrowsAsync<SharePointNotFoundException>(
            () => sut.DownloadFileAsync("Documents/reports", "gone.txt"));

        Assert.Contains("File 'Documents/reports/gone.txt' was not found", ex.Message);
    }

    [Fact]
    public async Task DownloadFileAsync_WhenUnexpectedError_ThrowsSharePointException()
    {
        var (sut, requestAdapter) = CreateSutWithDrive("Documents");

        requestAdapter
            .SendPrimitiveAsync<Stream>(
                Arg.Any<RequestInformation>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateAccessDeniedError());

        var ex = await Assert.ThrowsAsync<SharePointException>(
            () => sut.DownloadFileAsync("Documents", "a.txt"));

        Assert.Contains("Failed to download file 'Documents/a.txt'", ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DeleteFileAsync_InvalidFileName_ThrowsArgumentException(string? fileName)
    {
        var (sut, _) = CreateSutWithDrive("Documents");

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => sut.DeleteFileAsync("Documents", fileName!));
    }

    [Fact]
    public async Task DeleteFileAsync_DeletesItem()
    {
        var (sut, requestAdapter) = CreateSutWithDrive("Documents");
        requestAdapter
            .SendNoContentAsync(
                Arg.Any<RequestInformation>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await sut.DeleteFileAsync("Documents/reports", "a.txt");

        await requestAdapter.Received(1).SendNoContentAsync(
            Arg.Is<RequestInformation>(r => r.HttpMethod == Method.DELETE),
            Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteFileAsync_WhenNotFound_ThrowsNotFoundException()
    {
        var (sut, requestAdapter) = CreateSutWithDrive("Documents");

        requestAdapter
            .SendNoContentAsync(
                Arg.Any<RequestInformation>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateNotFoundError());

        var ex = await Assert.ThrowsAsync<SharePointNotFoundException>(
            () => sut.DeleteFileAsync("Documents", "gone.txt"));

        Assert.Contains("File 'Documents/gone.txt' was not found", ex.Message);
    }

    [Fact]
    public async Task DeleteFileAsync_WhenUnexpectedError_ThrowsSharePointException()
    {
        var (sut, requestAdapter) = CreateSutWithDrive("Documents");

        requestAdapter
            .SendNoContentAsync(
                Arg.Any<RequestInformation>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateAccessDeniedError());

        var ex = await Assert.ThrowsAsync<SharePointException>(
            () => sut.DeleteFileAsync("Documents/reports", "a.txt"));

        Assert.Contains("Failed to delete file 'Documents/reports/a.txt'", ex.Message);
    }

    [Fact]
    public async Task ResolveDriveIdAsync_WhenListingDrivesFails_ThrowsSharePointException()
    {
        var options = CreateOptions();
        var requestAdapter = CreateRequestAdapter();
        requestAdapter
            .SendAsync(
                Arg.Any<RequestInformation>(),
                Arg.Any<ParsableFactory<DriveCollectionResponse>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateAccessDeniedError());

        var sut = new GraphClientWrapper(options, new GraphServiceClient(requestAdapter));

        var ex = await Assert.ThrowsAsync<SharePointException>(
            () => sut.ResolveDriveIdAsync("Documents", CancellationToken.None));

        Assert.Contains("Failed to list document libraries", ex.Message);
    }

    [Fact]
    public async Task ResolveDriveIdAsync_WhenListingDrivesReturnsNotFound_ThrowsNotFoundException()
    {
        var options = CreateOptions();
        var requestAdapter = CreateRequestAdapter();
        requestAdapter
            .SendAsync(
                Arg.Any<RequestInformation>(),
                Arg.Any<ParsableFactory<DriveCollectionResponse>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateNotFoundError());

        var sut = new GraphClientWrapper(options, new GraphServiceClient(requestAdapter));

        var ex = await Assert.ThrowsAsync<SharePointNotFoundException>(
            () => sut.ResolveDriveIdAsync("Documents", CancellationToken.None));

        Assert.Contains("Failed to list document libraries", ex.Message);
    }

    [Fact]
    public async Task ResolveSiteIdAsync_WhenUnexpectedError_ThrowsSharePointException()
    {
        var options = CreateOptions(siteId: "", siteHostname: "contoso.sharepoint.com", sitePath: "/sites/MySite");
        var requestAdapter = CreateRequestAdapter();
        requestAdapter
            .SendAsync(
                Arg.Any<RequestInformation>(),
                Arg.Any<ParsableFactory<Site>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateAccessDeniedError());

        var sut = new GraphClientWrapper(options, new GraphServiceClient(requestAdapter));

        var ex = await Assert.ThrowsAsync<SharePointException>(
            () => sut.ResolveSiteIdAsync(CancellationToken.None));

        Assert.Contains("Failed to resolve SharePoint site", ex.Message);
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new GraphClientWrapper(null!, new GraphServiceClient(CreateRequestAdapter())));
    }

    [Fact]
    public void Constructor_NullGraphClient_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new GraphClientWrapper(CreateOptions(), null!));
    }

    private static (GraphClientWrapper Sut, IRequestAdapter RequestAdapter) CreateSutWithDrive(
        string libraryName,
        string driveId = "drive-1")
    {
        var options = CreateOptions();
        var requestAdapter = CreateRequestAdapter();
        requestAdapter
            .SendAsync(
                Arg.Any<RequestInformation>(),
                Arg.Any<ParsableFactory<DriveCollectionResponse>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>?>(),
                Arg.Any<CancellationToken>())
            .Returns(new DriveCollectionResponse
            {
                Value = [new Drive { Id = driveId, Name = libraryName }]
            });

        return (new GraphClientWrapper(options, new GraphServiceClient(requestAdapter)), requestAdapter);
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

    private static ODataError CreateAccessDeniedError() =>
        new()
        {
            Error = new MainError { Code = "accessDenied", Message = "denied" },
            ResponseStatusCode = 403
        };
}

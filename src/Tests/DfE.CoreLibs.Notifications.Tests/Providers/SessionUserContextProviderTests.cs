using DfE.CoreLibs.Notifications.Providers;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;

namespace DfE.CoreLibs.Notifications.Tests.Providers;

public class SessionUserContextProviderTests
{
    private readonly IHttpContextAccessor _mockHttpContextAccessor;
    private readonly SessionUserContextProvider _provider;

    public SessionUserContextProviderTests()
    {
        _mockHttpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _provider = new SessionUserContextProvider(_mockHttpContextAccessor);
    }

    [Fact]
    public void Constructor_WithNullHttpContextAccessor_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new SessionUserContextProvider(null!));
    }

    [Fact]
    public void GetCurrentUserId_WithHttpContextAndUserName_ShouldReturnUserName()
    {
        // Arrange
        var mockHttpContext = Substitute.For<HttpContext>();
        var mockUser = Substitute.For<System.Security.Claims.ClaimsPrincipal>();
        
        mockUser.Identity!.Name.Returns("test-user");
        mockHttpContext.User.Returns(mockUser);
        _mockHttpContextAccessor.HttpContext.Returns(mockHttpContext);

        // Act
        var result = _provider.GetCurrentUserId();

        // Assert
        Assert.Equal("test-user", result);
    }

    [Fact]
    public void GetCurrentUserId_WithoutHttpContext_ShouldReturnDefault()
    {
        // Arrange
        _mockHttpContextAccessor.HttpContext.Returns((HttpContext?)null);

        // Act
        var result = _provider.GetCurrentUserId();

        // Assert
        Assert.Equal("default", result);
    }

    [Fact]
    public void GetCurrentUserId_WithoutUser_ShouldReturnDefault()
    {
        // Arrange
        var mockHttpContext = Substitute.For<HttpContext>();
        mockHttpContext.User.Returns((System.Security.Claims.ClaimsPrincipal?)null);
        _mockHttpContextAccessor.HttpContext.Returns(mockHttpContext);

        // Act
        var result = _provider.GetCurrentUserId();

        // Assert
        Assert.Equal("default", result);
    }

    [Fact]
    public void GetCurrentUserId_WithUserButNoIdentity_ShouldReturnDefault()
    {
        // Arrange
        var mockHttpContext = Substitute.For<HttpContext>();
        var mockUser = Substitute.For<System.Security.Claims.ClaimsPrincipal>();
        
        mockUser.Identity.Returns((System.Security.Claims.ClaimsIdentity?)null);
        mockHttpContext.User.Returns(mockUser);
        _mockHttpContextAccessor.HttpContext.Returns(mockHttpContext);

        // Act
        var result = _provider.GetCurrentUserId();

        // Assert
        Assert.Equal("default", result);
    }

    [Fact]
    public void GetCurrentUserId_WithIdentityButNoName_ShouldReturnDefault()
    {
        // Arrange
        var mockHttpContext = Substitute.For<HttpContext>();
        var mockUser = Substitute.For<System.Security.Claims.ClaimsPrincipal>();
        
        mockUser.Identity!.Name.Returns((string?)null);
        mockHttpContext.User.Returns(mockUser);
        _mockHttpContextAccessor.HttpContext.Returns(mockHttpContext);

        // Act
        var result = _provider.GetCurrentUserId();

        // Assert
        Assert.Equal("default", result);
    }

    [Fact]
    public void IsContextAvailable_WithHttpContextAndUser_ShouldReturnTrue()
    {
        // Arrange
        var mockHttpContext = Substitute.For<HttpContext>();
        var mockUser = Substitute.For<System.Security.Claims.ClaimsPrincipal>();
        
        mockUser.Identity!.Name.Returns("test-user");
        mockHttpContext.User.Returns(mockUser);
        _mockHttpContextAccessor.HttpContext.Returns(mockHttpContext);

        // Act
        var result = _provider.IsContextAvailable();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsContextAvailable_WithoutHttpContext_ShouldReturnFalse()
    {
        // Arrange
        _mockHttpContextAccessor.HttpContext.Returns((HttpContext?)null);

        // Act
        var result = _provider.IsContextAvailable();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsContextAvailable_WithoutUser_ShouldReturnFalse()
    {
        // Arrange
        var mockHttpContext = Substitute.For<HttpContext>();
        mockHttpContext.User.Returns((System.Security.Claims.ClaimsPrincipal?)null);
        _mockHttpContextAccessor.HttpContext.Returns(mockHttpContext);

        // Act
        var result = _provider.IsContextAvailable();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsContextAvailable_WithUserButNoName_ShouldReturnFalse()
    {
        // Arrange
        var mockHttpContext = Substitute.For<HttpContext>();
        var mockUser = Substitute.For<System.Security.Claims.ClaimsPrincipal>();
        
        mockUser.Identity!.Name.Returns((string?)null);
        mockHttpContext.User.Returns(mockUser);
        _mockHttpContextAccessor.HttpContext.Returns(mockHttpContext);

        // Act
        var result = _provider.IsContextAvailable();

        // Assert
        Assert.False(result);
    }
}
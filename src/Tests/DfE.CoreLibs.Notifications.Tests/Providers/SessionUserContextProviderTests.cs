using DfE.CoreLibs.Notifications.Providers;
using Microsoft.AspNetCore.Http;
using NSubstitute;

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
    public void GetCurrentUserId_WithValidSession_ShouldReturnSessionId()
    {
        // Arrange
        const string expectedSessionId = "test-session-123";
        
        var mockSession = Substitute.For<ISession>();
        mockSession.Id.Returns(expectedSessionId);
        
        var mockHttpContext = Substitute.For<HttpContext>();
        mockHttpContext.Session.Returns(mockSession);
        
        _mockHttpContextAccessor.HttpContext.Returns(mockHttpContext);

        // Act
        var result = _provider.GetCurrentUserId();

        // Assert
        Assert.Equal(expectedSessionId, result);
    }

    [Fact]
    public void GetCurrentUserId_WithNullHttpContext_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockHttpContextAccessor.HttpContext.Returns((HttpContext?)null);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _provider.GetCurrentUserId());
        Assert.Equal("Session is not available", exception.Message);
    }

    [Fact]
    public void GetCurrentUserId_WithNullSession_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var mockHttpContext = Substitute.For<HttpContext>();
        mockHttpContext.Session.Returns((ISession?)null);
        
        _mockHttpContextAccessor.HttpContext.Returns(mockHttpContext);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _provider.GetCurrentUserId());
        Assert.Equal("Session is not available", exception.Message);
    }

    [Fact]
    public void IsContextAvailable_WithValidSession_ShouldReturnTrue()
    {
        // Arrange
        var mockSession = Substitute.For<ISession>();
        var mockHttpContext = Substitute.For<HttpContext>();
        mockHttpContext.Session.Returns(mockSession);
        
        _mockHttpContextAccessor.HttpContext.Returns(mockHttpContext);

        // Act
        var result = _provider.IsContextAvailable();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsContextAvailable_WithNullHttpContext_ShouldReturnFalse()
    {
        // Arrange
        _mockHttpContextAccessor.HttpContext.Returns((HttpContext?)null);

        // Act
        var result = _provider.IsContextAvailable();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsContextAvailable_WithNullSession_ShouldReturnFalse()
    {
        // Arrange
        var mockHttpContext = Substitute.For<HttpContext>();
        mockHttpContext.Session.Returns((ISession?)null);
        
        _mockHttpContextAccessor.HttpContext.Returns(mockHttpContext);

        // Act
        var result = _provider.IsContextAvailable();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Constructor_WithNullHttpContextAccessor_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new SessionUserContextProvider(null!));
        Assert.Equal("httpContextAccessor", exception.ParamName);
    }

    [Fact]
    public void IsContextAvailable_CalledMultipleTimes_ShouldBeConsistent()
    {
        // Arrange
        var mockSession = Substitute.For<ISession>();
        var mockHttpContext = Substitute.For<HttpContext>();
        mockHttpContext.Session.Returns(mockSession);
        
        _mockHttpContextAccessor.HttpContext.Returns(mockHttpContext);

        // Act
        var result1 = _provider.IsContextAvailable();
        var result2 = _provider.IsContextAvailable();

        // Assert
        Assert.True(result1);
        Assert.True(result2);
        Assert.Equal(result1, result2);
    }

    [Fact]
    public void GetCurrentUserId_CalledMultipleTimes_ShouldReturnSameValue()
    {
        // Arrange
        const string expectedSessionId = "consistent-session-id";
        
        var mockSession = Substitute.For<ISession>();
        mockSession.Id.Returns(expectedSessionId);
        
        var mockHttpContext = Substitute.For<HttpContext>();
        mockHttpContext.Session.Returns(mockSession);
        
        _mockHttpContextAccessor.HttpContext.Returns(mockHttpContext);

        // Act
        var result1 = _provider.GetCurrentUserId();
        var result2 = _provider.GetCurrentUserId();

        // Assert
        Assert.Equal(expectedSessionId, result1);
        Assert.Equal(expectedSessionId, result2);
        Assert.Equal(result1, result2);
    }
}
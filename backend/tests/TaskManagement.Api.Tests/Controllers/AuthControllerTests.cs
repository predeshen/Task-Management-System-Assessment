using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using TaskManagement.Api.Controllers;
using TaskManagement.Api.Models;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Models;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Api.Tests.Controllers;

[TestFixture]
public class AuthControllerTests
{
    private AuthController _controller;
    private Mock<IAuthService> _mockAuthService;
    private Mock<ILogger<AuthController>> _mockLogger;

    [SetUp]
    public void Setup()
    {
        _mockAuthService = new Mock<IAuthService>();
        _mockLogger = new Mock<ILogger<AuthController>>();
        _controller = new AuthController(_mockAuthService.Object, _mockLogger.Object);
        
        // Setup HttpContext for TraceIdentifier
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _controller.HttpContext.TraceIdentifier = "test-trace-id";
    }

    [Test]
    public async System.Threading.Tasks.Task Login_WhenValidCredentials_ShouldReturnOkWithToken()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "testpassword"
        };

        var user = new User
        {
            Id = 1,
            Username = "testuser",
            PasswordHash = "hashedpassword",
            CreatedAt = DateTime.UtcNow
        };

        var authResult = AuthResult.Successful("jwt-token", user);

        _mockAuthService.Setup(x => x.LoginAsync(request.Username, request.Password))
            .ReturnsAsync(authResult);

        // Act
        var result = await _controller.Login(request);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.Value, Is.InstanceOf<AuthResponse>());
        
        var response = (AuthResponse)okResult.Value!;
        Assert.That(response.Token, Is.EqualTo("jwt-token"));
        Assert.That(response.User.Id, Is.EqualTo(1));
        Assert.That(response.User.Username, Is.EqualTo("testuser"));
    }

    [Test]
    public async System.Threading.Tasks.Task Login_WhenInvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "wrongpassword"
        };

        var authResult = AuthResult.Failed("Invalid username or password.");

        _mockAuthService.Setup(x => x.LoginAsync(request.Username, request.Password))
            .ReturnsAsync(authResult);

        // Act
        var result = await _controller.Login(request);

        // Assert
        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
        var unauthorizedResult = (UnauthorizedObjectResult)result;
        Assert.That(unauthorizedResult.Value, Is.InstanceOf<ErrorResponse>());
        
        var errorResponse = (ErrorResponse)unauthorizedResult.Value!;
        Assert.That(errorResponse.Success, Is.False);
        Assert.That(errorResponse.Message, Is.EqualTo("Invalid username or password."));
        Assert.That(errorResponse.TraceId, Is.EqualTo("test-trace-id"));
    }

    [Test]
    public async System.Threading.Tasks.Task Login_WhenModelStateInvalid_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "",
            Password = "testpassword"
        };

        _controller.ModelState.AddModelError("Username", "Username is required");

        // Act
        var result = await _controller.Login(request);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = (BadRequestObjectResult)result;
        Assert.That(badRequestResult.Value, Is.InstanceOf<ErrorResponse>());
        
        var errorResponse = (ErrorResponse)badRequestResult.Value!;
        Assert.That(errorResponse.Success, Is.False);
        Assert.That(errorResponse.Message, Is.EqualTo("Validation failed"));
        Assert.That(errorResponse.Errors, Has.Count.EqualTo(1));
        Assert.That(errorResponse.Errors[0].Field, Is.EqualTo("Username"));
        Assert.That(errorResponse.Errors[0].Message, Is.EqualTo("Username is required"));
    }

    [Test]
    public async System.Threading.Tasks.Task Login_WhenExceptionThrown_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "testpassword"
        };

        _mockAuthService.Setup(x => x.LoginAsync(request.Username, request.Password))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _controller.Login(request);

        // Assert
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var objectResult = (ObjectResult)result;
        Assert.That(objectResult.StatusCode, Is.EqualTo(500));
        Assert.That(objectResult.Value, Is.InstanceOf<ErrorResponse>());
        
        var errorResponse = (ErrorResponse)objectResult.Value!;
        Assert.That(errorResponse.Success, Is.False);
        Assert.That(errorResponse.Message, Is.EqualTo("An error occurred during login"));
        Assert.That(errorResponse.TraceId, Is.EqualTo("test-trace-id"));
    }

    [Test]
    public async System.Threading.Tasks.Task Login_WhenMultipleValidationErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "",
            Password = ""
        };

        _controller.ModelState.AddModelError("Username", "Username is required");
        _controller.ModelState.AddModelError("Password", "Password is required");

        // Act
        var result = await _controller.Login(request);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = (BadRequestObjectResult)result;
        var errorResponse = (ErrorResponse)badRequestResult.Value!;
        
        Assert.That(errorResponse.Errors, Has.Count.EqualTo(2));
        Assert.That(errorResponse.Errors.Any(e => e.Field == "Username" && e.Message == "Username is required"), Is.True);
        Assert.That(errorResponse.Errors.Any(e => e.Field == "Password" && e.Message == "Password is required"), Is.True);
    }

    [Test]
    public void Constructor_WhenNullAuthService_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AuthController(null, _mockLogger.Object));
    }

    [Test]
    public void Constructor_WhenNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AuthController(_mockAuthService.Object, null));
    }

    [Test]
    public async System.Threading.Tasks.Task Login_WhenSuccessful_ShouldLogInformation()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "testpassword"
        };

        var user = new User
        {
            Id = 1,
            Username = "testuser",
            PasswordHash = "hashedpassword",
            CreatedAt = DateTime.UtcNow
        };

        var authResult = AuthResult.Successful("jwt-token", user);

        _mockAuthService.Setup(x => x.LoginAsync(request.Username, request.Password))
            .ReturnsAsync(authResult);

        // Act
        await _controller.Login(request);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("logged in successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async System.Threading.Tasks.Task Login_WhenExceptionOccurs_ShouldLogError()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "testpassword"
        };

        var exception = new Exception("Database connection failed");
        _mockAuthService.Setup(x => x.LoginAsync(request.Username, request.Password))
            .ThrowsAsync(exception);

        // Act
        await _controller.Login(request);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error occurred during login")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
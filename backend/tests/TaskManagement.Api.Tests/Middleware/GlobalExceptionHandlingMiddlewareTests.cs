using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System.Net;
using System.Text.Json;
using TaskManagement.Api.Middleware;

namespace TaskManagement.Api.Tests.Middleware;

[TestFixture]
public class GlobalExceptionHandlingMiddlewareTests
{
    private GlobalExceptionHandlingMiddleware _middleware = null!;
    private ILogger<GlobalExceptionHandlingMiddleware> _logger = null!;
    private DefaultHttpContext _context = null!;

    [SetUp]
    public void Setup()
    {
        _logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<GlobalExceptionHandlingMiddleware>();
        _context = new DefaultHttpContext();
        _context.Response.Body = new MemoryStream();
    }

    [Test]
    public async System.Threading.Tasks.Task InvokeAsync_WhenNoException_ShouldCallNext()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = (HttpContext context) =>
        {
            nextCalled = true;
            return System.Threading.Tasks.Task.CompletedTask;
        };

        _middleware = new GlobalExceptionHandlingMiddleware(next, _logger);

        // Act
        await _middleware.InvokeAsync(_context);

        // Assert
        Assert.That(nextCalled, Is.True);
    }

    [Test]
    public async System.Threading.Tasks.Task InvokeAsync_WhenArgumentException_ShouldReturnBadRequest()
    {
        // Arrange
        var exceptionMessage = "Test argument exception";
        RequestDelegate next = (HttpContext context) =>
        {
            throw new ArgumentException(exceptionMessage);
        };

        _middleware = new GlobalExceptionHandlingMiddleware(next, _logger);

        // Act
        await _middleware.InvokeAsync(_context);

        // Assert
        Assert.That(_context.Response.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
        Assert.That(_context.Response.ContentType, Is.EqualTo("application/json"));

        _context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_context.Response.Body).ReadToEndAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.That(errorResponse, Is.Not.Null);
        Assert.That(errorResponse!.StatusCode, Is.EqualTo(400));
        Assert.That(errorResponse.Message, Is.EqualTo("Invalid request parameters"));
        Assert.That(errorResponse.Details, Is.EqualTo(exceptionMessage));
    }

    [Test]
    public async System.Threading.Tasks.Task InvokeAsync_WhenUnauthorizedAccessException_ShouldReturnUnauthorized()
    {
        // Arrange
        var exceptionMessage = "Test unauthorized exception";
        RequestDelegate next = (HttpContext context) =>
        {
            throw new UnauthorizedAccessException(exceptionMessage);
        };

        _middleware = new GlobalExceptionHandlingMiddleware(next, _logger);

        // Act
        await _middleware.InvokeAsync(_context);

        // Assert
        Assert.That(_context.Response.StatusCode, Is.EqualTo((int)HttpStatusCode.Unauthorized));

        _context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_context.Response.Body).ReadToEndAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.That(errorResponse, Is.Not.Null);
        Assert.That(errorResponse!.StatusCode, Is.EqualTo(401));
        Assert.That(errorResponse.Message, Is.EqualTo("Unauthorized access"));
        Assert.That(errorResponse.Details, Is.EqualTo(exceptionMessage));
    }

    [Test]
    public async System.Threading.Tasks.Task InvokeAsync_WhenKeyNotFoundException_ShouldReturnNotFound()
    {
        // Arrange
        var exceptionMessage = "Test not found exception";
        RequestDelegate next = (HttpContext context) =>
        {
            throw new KeyNotFoundException(exceptionMessage);
        };

        _middleware = new GlobalExceptionHandlingMiddleware(next, _logger);

        // Act
        await _middleware.InvokeAsync(_context);

        // Assert
        Assert.That(_context.Response.StatusCode, Is.EqualTo((int)HttpStatusCode.NotFound));

        _context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_context.Response.Body).ReadToEndAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.That(errorResponse, Is.Not.Null);
        Assert.That(errorResponse!.StatusCode, Is.EqualTo(404));
        Assert.That(errorResponse.Message, Is.EqualTo("Resource not found"));
        Assert.That(errorResponse.Details, Is.EqualTo(exceptionMessage));
    }

    [Test]
    public async System.Threading.Tasks.Task InvokeAsync_WhenInvalidOperationException_ShouldReturnBadRequest()
    {
        // Arrange
        var exceptionMessage = "Test invalid operation exception";
        RequestDelegate next = (HttpContext context) =>
        {
            throw new InvalidOperationException(exceptionMessage);
        };

        _middleware = new GlobalExceptionHandlingMiddleware(next, _logger);

        // Act
        await _middleware.InvokeAsync(_context);

        // Assert
        Assert.That(_context.Response.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));

        _context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_context.Response.Body).ReadToEndAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.That(errorResponse, Is.Not.Null);
        Assert.That(errorResponse!.StatusCode, Is.EqualTo(400));
        Assert.That(errorResponse.Message, Is.EqualTo("Invalid operation"));
        Assert.That(errorResponse.Details, Is.EqualTo(exceptionMessage));
    }

    [Test]
    public async System.Threading.Tasks.Task InvokeAsync_WhenGenericException_ShouldReturnInternalServerError()
    {
        // Arrange
        var exceptionMessage = "Test generic exception";
        RequestDelegate next = (HttpContext context) =>
        {
            throw new Exception(exceptionMessage);
        };

        _middleware = new GlobalExceptionHandlingMiddleware(next, _logger);

        // Act
        await _middleware.InvokeAsync(_context);

        // Assert
        Assert.That(_context.Response.StatusCode, Is.EqualTo((int)HttpStatusCode.InternalServerError));

        _context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_context.Response.Body).ReadToEndAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.That(errorResponse, Is.Not.Null);
        Assert.That(errorResponse!.StatusCode, Is.EqualTo(500));
        Assert.That(errorResponse.Message, Is.EqualTo("An internal server error occurred"));
        Assert.That(errorResponse.Details, Is.EqualTo("Please contact support if the problem persists"));
    }

    [Test]
    public async System.Threading.Tasks.Task InvokeAsync_WhenExceptionThrown_ShouldIncludeTimestampAndTraceId()
    {
        // Arrange
        RequestDelegate next = (HttpContext context) =>
        {
            throw new Exception("Test exception");
        };

        _middleware = new GlobalExceptionHandlingMiddleware(next, _logger);

        // Act
        var beforeTime = DateTime.UtcNow;
        await _middleware.InvokeAsync(_context);
        var afterTime = DateTime.UtcNow;

        // Assert
        _context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_context.Response.Body).ReadToEndAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.That(errorResponse, Is.Not.Null);
        Assert.That(errorResponse!.Timestamp, Is.GreaterThanOrEqualTo(beforeTime));
        Assert.That(errorResponse.Timestamp, Is.LessThanOrEqualTo(afterTime));
        Assert.That(errorResponse.TraceId, Is.Not.Null.And.Not.Empty);
    }
}
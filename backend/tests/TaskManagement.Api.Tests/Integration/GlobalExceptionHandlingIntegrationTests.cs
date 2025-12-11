using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using System.Net;
using System.Text.Json;
using TaskManagement.Api.Middleware;
using TaskManagement.Api.Models;

namespace TaskManagement.Api.Tests.Integration;

[TestFixture]
public class GlobalExceptionHandlingIntegrationTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;

    [SetUp]
    public void Setup()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
            });
        
        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async System.Threading.Tasks.Task ExceptionEndpoint_WhenArgumentException_ShouldReturnBadRequestWithErrorResponse()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/test/exception/argument");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));

        var content = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.That(errorResponse, Is.Not.Null);
        Assert.That(errorResponse!.Message, Is.EqualTo("Invalid request parameters"));
        Assert.That(errorResponse.Errors, Is.Not.Empty);
        Assert.That(errorResponse.Errors.First().Message, Is.EqualTo("This is a test argument exception"));
        Assert.That(errorResponse.TraceId, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async System.Threading.Tasks.Task ExceptionEndpoint_WhenUnauthorizedException_ShouldReturnUnauthorizedWithErrorResponse()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/test/exception/unauthorized");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        var content = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.That(errorResponse, Is.Not.Null);
        Assert.That(errorResponse!.Message, Is.EqualTo("Unauthorized access"));
        Assert.That(errorResponse.Errors, Is.Not.Empty);
        Assert.That(errorResponse.Errors.First().Message, Is.EqualTo("This is a test unauthorized exception"));
    }

    [Test]
    public async System.Threading.Tasks.Task ExceptionEndpoint_WhenNotFoundException_ShouldReturnNotFoundWithErrorResponse()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/test/exception/notfound");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

        var content = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.That(errorResponse, Is.Not.Null);
        Assert.That(errorResponse!.Message, Is.EqualTo("Resource not found"));
        Assert.That(errorResponse.Errors, Is.Not.Empty);
        Assert.That(errorResponse.Errors.First().Message, Is.EqualTo("This is a test not found exception"));
    }

    [Test]
    public async System.Threading.Tasks.Task ExceptionEndpoint_WhenInvalidOperationException_ShouldReturnBadRequestWithErrorResponse()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/test/exception/invalid");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var content = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.That(errorResponse, Is.Not.Null);
        Assert.That(errorResponse!.Message, Is.EqualTo("Invalid operation"));
        Assert.That(errorResponse.Errors, Is.Not.Empty);
        Assert.That(errorResponse.Errors.First().Message, Is.EqualTo("This is a test invalid operation exception"));
    }

    [Test]
    public async System.Threading.Tasks.Task ExceptionEndpoint_WhenGenericException_ShouldReturnInternalServerErrorWithErrorResponse()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/test/exception/server");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

        var content = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.That(errorResponse, Is.Not.Null);
        Assert.That(errorResponse!.Message, Is.EqualTo("An internal server error occurred"));
        Assert.That(errorResponse.Errors, Is.Not.Empty);
        Assert.That(errorResponse.Errors.First().Message, Is.EqualTo("Please contact support if the problem persists"));
    }

    [Test]
    public async System.Threading.Tasks.Task ExceptionEndpoint_WhenInvalidExceptionType_ShouldReturnBadRequestWithErrorResponse()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/test/exception/invalid-type");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var content = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.That(errorResponse, Is.Not.Null);
        Assert.That(errorResponse!.Message, Is.EqualTo("Invalid request parameters"));
        Assert.That(errorResponse.Errors, Is.Not.Empty);
        Assert.That(errorResponse.Errors.First().Message, Contains.Substring("Invalid exception type"));
    }

    [Test]
    public async System.Threading.Tasks.Task NormalEndpoint_WhenNoException_ShouldWorkNormally()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/test/public");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Contains.Substring("This is a public endpoint"));
    }
}